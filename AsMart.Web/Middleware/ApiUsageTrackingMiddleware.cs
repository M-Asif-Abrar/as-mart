using AsMart.Web.Data;
using AsMart.Web.Models;
using AsMart.Web.Middleware;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace AsMart.Web.Middleware
{
    public sealed class ApiUsageTrackingMiddleware
    {
        private const int MaximumQueryStringLength = 500;
        private const int MaximumUserAgentLength = 500;
        private const int MaximumIpAddressLength = 100;

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

                await SaveUsageLogAsync(
                    context,
                    scopeFactory,
                    stopwatch.ElapsedMilliseconds);
            }
        }

        private async Task SaveUsageLogAsync(
            HttpContext context,
            IServiceScopeFactory scopeFactory,
            long responseTimeMs)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();

                var db = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();

                var apiClientId = GetIntItem(
                    context,
                    ApiKeyMiddleware.ApiClientIdItem);

                var userId = ResolveUserId(context);

                var apiVersion = ResolveApiVersion(
                    context.Request.Path);

                var usageLog = new ApiUsageLog
                {
                    ApiClientId = apiClientId,
                    UserId = Truncate(
                        userId,
                        128),

                    HttpMethod = Truncate(
                            context.Request.Method,
                            20)
                        ?? string.Empty,

                    ApiVersion = apiVersion,

                    Endpoint = Truncate(
                            context.Request.Path.Value,
                            500)
                        ?? string.Empty,

                    QueryString = context.Request
                        .QueryString
                        .HasValue
                            ? Truncate(
                                context.Request.QueryString.Value,
                                MaximumQueryStringLength)
                            : null,

                    StatusCode = context.Response.StatusCode,

                    ResponseTimeMs = responseTimeMs,

                    IpAddress = Truncate(
                        context.Connection
                            .RemoteIpAddress?
                            .ToString(),
                        MaximumIpAddressLength),

                    UserAgent = Truncate(
                        context.Request
                            .Headers
                            .UserAgent
                            .ToString(),
                        MaximumUserAgentLength),

                    CreatedAt = DateTime.UtcNow
                };

                db.ApiUsageLogs.Add(usageLog);

                await db.SaveChangesAsync(
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to save API usage log for {Method} {Path}. TraceId: {TraceId}",
                    context.Request.Method,
                    context.Request.Path,
                    context.TraceIdentifier);
            }
        }

        private static string ResolveApiVersion(
            PathString requestPath)
        {
            return requestPath.StartsWithSegments("/api/v1")
                ? "v1"
                : "legacy";
        }

        private static string? ResolveUserId(
            HttpContext context)
        {
            var apiKeyUserId = GetStringItem(
                context,
                ApiKeyMiddleware.ApiUserIdItem);

            if (!string.IsNullOrWhiteSpace(apiKeyUserId))
            {
                return apiKeyUserId;
            }

            /*
             * This fallback supports future JWT-authenticated
             * or cookie-authenticated API requests.
             */
            return context.User.FindFirstValue(
                ClaimTypes.NameIdentifier);
        }

        private static int? GetIntItem(
            HttpContext context,
            string key)
        {
            if (!context.Items.TryGetValue(
                    key,
                    out var value))
            {
                return null;
            }

            if (value is int intValue)
            {
                return intValue;
            }

            return int.TryParse(
                value?.ToString(),
                out var parsedValue)
                    ? parsedValue
                    : null;
        }

        private static string? GetStringItem(
            HttpContext context,
            string key)
        {
            return context.Items.TryGetValue(
                    key,
                    out var value)
                ? value?.ToString()
                : null;
        }

        private static string? Truncate(
            string? value,
            int maximumLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            value = value.Trim();

            return value.Length <= maximumLength
                ? value
                : value[..maximumLength];
        }
    }
}