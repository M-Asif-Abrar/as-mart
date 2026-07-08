using System.Threading.RateLimiting;
using AsMart.Web.Data;
using AsMart.Web.Models.Entities;
using AsMart.Web.Services;
using AsMart.Web.Services.Email;
using AsMart.Web.Services.Repositories;
using AsMart.Web.Services.Repositories.ErrorPages;
using AsMart.Web.Services.Repositories.Redirects;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using AsMart.Web.Services.Marketing;
using AsMart.Web.Middleware;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

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
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
        options.SignInScheme = IdentityConstants.ExternalScheme;
    });

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();

builder.Services.Configure<AmazonAffiliateOptions>(builder.Configuration.GetSection("AmazonAffiliate"));
builder.Services.Configure<FacebookGraphOptions>(builder.Configuration.GetSection("FacebookGraph"));

builder.Services.AddScoped<IAffiliateLinkService, AffiliateLinkService>();
builder.Services.AddScoped<ISlugService, SlugService>();
builder.Services.AddScoped<IParentCategoryRepository, ParentCategoryRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IBlogRepository, BlogRepository>();
builder.Services.AddScoped<AsMart.Web.Services.IColorService, AsMart.Web.Services.ColorService>();
builder.Services.AddScoped<SeoProductSelector>();

builder.Services.AddScoped<RedirectRuleRepository>();
builder.Services.AddScoped<ErrorLogRepository>();

builder.Services.AddScoped<IUtmTrackingService, UtmTrackingService>();

builder.Services.AddHttpClient();

builder.Services.AddScoped<IFacebookPagePublisher, FacebookPagePublisher>();
builder.Services.AddHostedService<MarketingQueueAutoPublisherWorker>();

builder.Services.AddScoped<IApiKeyService, ApiKeyService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AsMartPublicApi", policy =>
    {
        policy
            .WithOrigins(
                "https://asifabrar.net",
                "https://www.asifabrar.net"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = (context, token) =>
    {
        var http = context.HttpContext;

        if (!http.Response.HasStarted)
        {
            http.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            http.Response.Headers["Retry-After"] = "60";
            http.Response.Headers["Cache-Control"] = "no-store";
        }

        return ValueTask.CompletedTask;
    };

    options.AddPolicy("public-api", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var clientId = httpContext.Items["ApiClientId"]?.ToString();
        var rateLimit = httpContext.Items["ApiRateLimit"] as int? ?? 60;

        if (!string.IsNullOrWhiteSpace(clientId))
        {
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: $"apikey:{clientId}",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = rateLimit,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
        }

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"anonymous:{ip}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

    app.Use(async (context, next) =>
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        Console.WriteLine($"START {context.Request.Method} {context.Request.Path}");

        await next();

        sw.Stop();

        Console.WriteLine($"END {context.Request.Method} {context.Request.Path} => {context.Response.StatusCode} in {sw.ElapsedMilliseconds} ms");
    });
}
else
{
    app.UseExceptionHandler("/error/500");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseCors("AsMartPublicApi");

app.UseMiddleware<ApiKeyMiddleware>();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    var enabled = app.Configuration.GetValue<bool>("MaintenanceMode:Enabled");

    if (!enabled)
    {
        await next();
        return;
    }

    var path = context.Request.Path.Value ?? "";

    if (path.StartsWith("/error", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/admin", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/identity", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/account", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/css", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/js", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/images", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/Pictures", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
    {
        await next();
        return;
    }

    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;

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

app.UseStatusCodePagesWithReExecute("/error/{0}");

app.MapControllers();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();