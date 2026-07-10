using AsMart.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace AsMart.Web.Middleware
{
    public class ApiQuotaMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiQuotaMiddleware> _logger;

        public ApiQuotaMiddleware(
            RequestDelegate next,
            ILogger<ApiQuotaMiddleware> logger)
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

            var clientId = GetIntItem(
                context,
                ApiKeyMiddleware.ApiClientIdItem);

            /*
             * Anonymous/public API calls are controlled by the
             * per-minute rate limiter, not client monthly quota.
             */
            if (!clientId.HasValue)
            {
                await _next(context);
                return;
            }

            var monthlyQuota = GetIntItem(
                context,
                ApiKeyMiddleware.ApiMonthlyQuotaItem);

            /*
             * Null or zero quota means unlimited.
             * Change this behavior if you want zero to block all usage.
             */
            if (!monthlyQuota.HasValue || monthlyQuota.Value <= 0)
            {
                await _next(context);
                return;
            }

            var now = DateTime.UtcNow;

            var monthStart = new DateTime(
                now.Year,
                now.Month,
                1,
                0,
                0,
                0,
                DateTimeKind.Utc);

            var nextMonth = monthStart.AddMonths(1);

            using var scope = scopeFactory.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var requestsUsed = await db.ApiUsageLogs
                .AsNoTracking()
                .CountAsync(x =>
                    x.ApiClientId == clientId.Value &&
                    x.CreatedAt >= monthStart &&
                    x.CreatedAt < nextMonth);

            SetQuotaHeaders(
                context,
                monthlyQuota.Value,
                requestsUsed,
                nextMonth);

            if (requestsUsed >= monthlyQuota.Value)
            {
                _logger.LogWarning(
                    "Monthly API quota exceeded. ClientId: {ClientId}, Used: {Used}, Quota: {Quota}",
                    clientId.Value,
                    requestsUsed,
                    monthlyQuota.Value);

                context.Response.StatusCode =
                    StatusCodes.Status429TooManyRequests;

                context.Response.ContentType =
                    "application/json; charset=utf-8";

                context.Response.Headers["Cache-Control"] = "no-store";

                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    statusCode = StatusCodes.Status429TooManyRequests,
                    message = "Monthly API quota exceeded.",
                    quota = monthlyQuota.Value,
                    used = requestsUsed,
                    remaining = 0,
                    resetAt = nextMonth
                });

                return;
            }

            await _next(context);
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

        private static void SetQuotaHeaders(
            HttpContext context,
            int quota,
            int used,
            DateTime resetAt)
        {
            context.Response.Headers["X-Monthly-Quota-Limit"] =
                quota.ToString();

            context.Response.Headers["X-Monthly-Quota-Used"] =
                used.ToString();

            context.Response.Headers["X-Monthly-Quota-Remaining"] =
                Math.Max(quota - used, 0).ToString();

            context.Response.Headers["X-Monthly-Quota-Reset"] =
                resetAt.ToString("O");
        }
    }
}