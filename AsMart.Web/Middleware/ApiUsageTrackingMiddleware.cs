using AsMart.Web.Data;
using AsMart.Web.Models;
using System.Diagnostics;

namespace AsMart.Web.Middleware
{
    public class ApiUsageTrackingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiUsageTrackingMiddleware> _logger;

        public ApiUsageTrackingMiddleware(
            RequestDelegate next,
            ILogger<ApiUsageTrackingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(
            HttpContext context,
            IServiceScopeFactory scopeFactory)
        {
            if (!context.Request.Path.StartsWithSegments("/api"))
            {
                await _next(context);
                return;
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();

                try
                {
                    using var scope = scopeFactory.CreateScope();

                    var db = scope.ServiceProvider
                        .GetRequiredService<ApplicationDbContext>();

                    var apiClientId = GetIntItem(
                        context,
                        ApiKeyMiddleware.ApiClientIdItem);

                    var userId = GetStringItem(
                        context,
                        ApiKeyMiddleware.ApiUserIdItem);

                    var log = new ApiUsageLog
                    {
                        ApiClientId = apiClientId,
                        UserId = userId,
                        HttpMethod = context.Request.Method,
                        Endpoint = context.Request.Path.Value
                            ?? string.Empty,
                        QueryString = context.Request.QueryString.HasValue
                            ? context.Request.QueryString.Value
                            : null,
                        StatusCode = context.Response.StatusCode,
                        ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                        IpAddress = context.Connection
                            .RemoteIpAddress?
                            .ToString(),
                        UserAgent = context.Request
                            .Headers
                            .UserAgent
                            .ToString(),
                        CreatedAt = DateTime.UtcNow
                    };

                    db.ApiUsageLogs.Add(log);
                    await db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to save API usage log for {Method} {Path}.",
                        context.Request.Method,
                        context.Request.Path);
                }
            }
        }

        private static int? GetIntItem(
            HttpContext context,
            string key)
        {
            if (!context.Items.TryGetValue(key, out var value))
            {
                return null;
            }

            if (value is int intValue)
            {
                return intValue;
            }

            return int.TryParse(value?.ToString(), out var parsedValue)
                ? parsedValue
                : null;
        }

        private static string? GetStringItem(
            HttpContext context,
            string key)
        {
            return context.Items.TryGetValue(key, out var value)
                ? value?.ToString()
                : null;
        }
    }
}