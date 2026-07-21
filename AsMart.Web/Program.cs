using AsMart.Web.Data;
using AsMart.Web.Middleware;
using AsMart.Web.Models.Api;
using AsMart.Web.Models.Entities;
using AsMart.Web.Services;
using AsMart.Web.Services.Email;
using AsMart.Web.Services.Marketing;
using AsMart.Web.Services.Repositories;
using AsMart.Web.Services.Repositories.ErrorPages;
using AsMart.Web.Services.Repositories.Redirects;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using System.Reflection;
using System.Threading.RateLimiting;
using AsMart.Web.Models.Security;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddMemoryCache();

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddDefaultUI();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/error/403";
});

builder.Services
    .AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId =
            builder.Configuration["Authentication:Google:ClientId"]!;

        options.ClientSecret =
            builder.Configuration["Authentication:Google:ClientSecret"]!;

        options.SignInScheme = IdentityConstants.ExternalScheme;
    });

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("Email"));

builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();

builder.Services.Configure<AmazonAffiliateOptions>(
    builder.Configuration.GetSection("AmazonAffiliate"));

builder.Services.Configure<FacebookGraphOptions>(
    builder.Configuration.GetSection("FacebookGraph"));

builder.Services.AddScoped<IAffiliateLinkService, AffiliateLinkService>();
builder.Services.AddScoped<ISlugService, SlugService>();
builder.Services.AddScoped<IParentCategoryRepository, ParentCategoryRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IBlogRepository, BlogRepository>();
builder.Services.AddScoped<IColorService, ColorService>();
builder.Services.AddScoped<SeoProductSelector>();

builder.Services.AddScoped<RedirectRuleRepository>();
builder.Services.AddScoped<ErrorLogRepository>();
builder.Services.AddScoped<IUtmTrackingService, UtmTrackingService>();

builder.Services.AddHttpClient();

builder.Services.AddScoped<IFacebookPagePublisher, FacebookPagePublisher>();
builder.Services.AddHostedService<MarketingQueueAutoPublisherWorker>();

builder.Services
    .AddOptions<ApiKeySecurityOptions>()
    .Bind(
        builder.Configuration.GetSection(
            ApiKeySecurityOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(
        options =>
            !string.IsNullOrWhiteSpace(
                options.HashingPepper) &&
            options.HashingPepper.Length >= 32,
        "ApiKeySecurity:HashingPepper must contain at least 32 characters.")
    .ValidateOnStart();

builder.Services.AddScoped<IApiKeyService,ApiKeyService>();
builder.Services.AddHostedService<ApiKeyLegacyBackfillHostedService>();


builder.Services.AddCors(options =>
{
    options.AddPolicy("AsMartPublicApi", policy =>
    {
        policy
            .WithOrigins(
                "https://asifabrar.net",
                "https://www.asifabrar.net",
                "https://localhost:44300",
                "https://localhost:5001")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services
    .AddControllersWithViews()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .ToDictionary(
                    entry => ToCamelCase(entry.Key),
                    entry => entry.Value!.Errors
                        .Select(error =>
                            string.IsNullOrWhiteSpace(error.ErrorMessage)
                                ? "The supplied value is invalid."
                                : error.ErrorMessage)
                        .Distinct()
                        .ToArray());

            var response = ApiResponseFactory.Error(
                ApiErrorCodes.ValidationFailed,
                "One or more validation errors occurred.",
                context.HttpContext.TraceIdentifier,
                errors);

            return new BadRequestObjectResult(response);
        };
    });

builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "AS-Mart Public API",
            Version = "v1",
            Description =
                """
                Public JSON API for AS-Mart products, categories, collections,
                blogs, and SEO guide pages.

                Requests can be made without an API key and will use the
                public rate limit.

                Registered integrations can provide an API key using the
                X-API-Key header to receive their assigned per-minute limit
                and monthly quota.
                """,
            Contact = new OpenApiContact
            {
                Name = "AS-Mart API Support",
                Url = new Uri("https://as-mart.com/api-documentation")
            }
        });

    options.TagActionsBy(api =>
    {
        var controllerName = api.ActionDescriptor.RouteValues
            .TryGetValue("controller", out var controller)
                ? controller
                : null;

        var tagName = string.IsNullOrWhiteSpace(controllerName)
            ? "API"
            : controllerName.Replace(
                "Api",
                string.Empty,
                StringComparison.OrdinalIgnoreCase);

        return new[] { tagName };
    });

    // Show only actual JSON API routes in Swagger.
    options.DocInclusionPredicate((_, apiDescription) =>
    {
        var relativePath = apiDescription.RelativePath;

        return !string.IsNullOrWhiteSpace(relativePath) &&
               relativePath.StartsWith(
                   "api/",
                   StringComparison.OrdinalIgnoreCase);
    });

    const string apiKeySchemeName = "ApiKey";

    options.AddSecurityDefinition(
        apiKeySchemeName,
        new OpenApiSecurityScheme
        {
            Name = "X-API-Key",
            Description =
                """
                Optional AS-Mart API key.

                Enter only the complete raw API key value.

                Example:
                asmart_xxxxxxxxxxxxxxxxxxxxxxxxxxxxx

                Swagger will send it using the X-API-Key header.
                """,
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = apiKeySchemeName
        });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(
                apiKeySchemeName,
                document)] = []
        });

    var xmlFileName =
        $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";

    var xmlFilePath = Path.Combine(
        AppContext.BaseDirectory,
        xmlFileName);

    if (File.Exists(xmlFilePath))
    {
        options.IncludeXmlComments(
            xmlFilePath,
            includeControllerXmlComments: true);
    }

    options.CustomSchemaIds(type =>
        type.FullName?.Replace("+", ".") ?? type.Name);
});


builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, cancellationToken) =>
    {
        var httpContext = context.HttpContext;

        if (httpContext.Response.HasStarted)
        {
            return;
        }

        var retryAfterSeconds = 60;

        if (context.Lease.TryGetMetadata(
                MetadataName.RetryAfter,
                out var retryAfter))
        {
            retryAfterSeconds = Math.Max(
                1,
                (int)Math.Ceiling(retryAfter.TotalSeconds));
        }

        httpContext.Response.Clear();
        httpContext.Response.StatusCode =
            StatusCodes.Status429TooManyRequests;
        httpContext.Response.ContentType =
            "application/json; charset=utf-8";
        httpContext.Response.Headers.CacheControl = "no-store";
        httpContext.Response.Headers.RetryAfter =
            retryAfterSeconds.ToString();

        var response = ApiResponseFactory.Error<object>(
            ApiErrorCodes.RateLimitExceeded,
            "The per-minute API request limit has been exceeded.",
            httpContext.TraceIdentifier,
            meta: new
            {
                retryAfterSeconds
            });

        await httpContext.Response.WriteAsJsonAsync(
            response,
            cancellationToken);
    };

    options.AddPolicy("public-api", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";

        var clientId = httpContext.Items[
            ApiKeyMiddleware.ApiClientIdItem]?.ToString();

        var rateLimit = GetPositiveIntItem(
            httpContext,
            ApiKeyMiddleware.ApiRateLimitItem,
            fallback: 60);

        if (!string.IsNullOrWhiteSpace(clientId))
        {
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: $"apikey:{clientId}",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = rateLimit,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder =
                        QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
        }

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"anonymous:{ip}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder =
                    QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error/500");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseSwagger(options =>
{
    options.RouteTemplate =
        "swagger/{documentName}/swagger.json";
});

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "AS-Mart Public API v1");

    options.RoutePrefix = "swagger";
    options.DocumentTitle = "AS-Mart API Explorer";
    options.DisplayRequestDuration();
    options.EnableDeepLinking();
    options.EnableFilter();
    options.EnablePersistAuthorization();

    options.DocExpansion(
        Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);

    options.DefaultModelsExpandDepth(1);
    options.DefaultModelExpandDepth(1);
    options.InjectStylesheet("/css/swagger-custom.css");
});

app.UseRouting();
app.UseCors("AsMartPublicApi");
app.UseAuthentication();

app.UseMiddleware<LegacyApiVersionMiddleware>();

app.UseMiddleware<ApiExceptionMiddleware>();

app.UseMiddleware<ApiUsageTrackingMiddleware>();

app.UseMiddleware<ApiKeyMiddleware>();

app.UseMiddleware<ApiQuotaMiddleware>();

app.UseRateLimiter();

app.UseAuthorization();

app.UseMiddleware<ApiNotFoundMiddleware>();

app.Use(async (context, next) =>
{
    var enabled = app.Configuration.GetValue<bool>(
        "MaintenanceMode:Enabled");

    if (!enabled)
    {
        await next();
        return;
    }

    var path = context.Request.Path.Value ?? string.Empty;

    if (path.StartsWith("/error", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/admin", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/identity", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/account", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/css", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/js", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/images", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/Pictures", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/api", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
    {
        await next();
        return;
    }

    context.Response.StatusCode =
        StatusCodes.Status503ServiceUnavailable;

    var originalPath = context.Request.Path;
    var originalQuery = context.Request.QueryString;

    context.Request.Path = "/error/503";
    context.Request.QueryString = QueryString.Empty;

    await next();

    context.Request.Path = originalPath;
    context.Request.QueryString = originalQuery;
});

if (!app.Environment.IsDevelopment())
{
    app.UseMiddleware<DbRedirectMiddleware>();
}

/*
 * Render HTML status pages only for non-API browser routes.
 */
app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/api"),
    branch =>
    {
        branch.UseStatusCodePagesWithReExecute("/error/{0}");
    });

app.MapControllers();

app.MapControllerRoute(
    name: "areas",
    pattern:
        "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern:
        "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();

static string ToCamelCase(string value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return value;
    }

    return char.ToLowerInvariant(value[0]) + value[1..];
}

static int GetPositiveIntItem(
    HttpContext context,
    string key,
    int fallback)
{
    if (!context.Items.TryGetValue(key, out var value))
    {
        return fallback;
    }

    if (value is int intValue && intValue > 0)
    {
        return intValue;
    }

    return int.TryParse(value?.ToString(), out var parsedValue) &&
           parsedValue > 0
        ? parsedValue
        : fallback;
}