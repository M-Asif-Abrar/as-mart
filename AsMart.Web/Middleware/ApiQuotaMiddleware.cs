using AsMart.Web.Data;
using AsMart.Web.Models.Api;
using AsMart.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace AsMart.Web.Middleware
{
    public sealed class ApiQuotaMiddleware
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
            ApplicationDbContext db)
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
             * Anonymous requests do not have a monthly client quota.
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
             * Zero or negative currently means unlimited.
             */
            if (!monthlyQuota.HasValue ||
                monthlyQuota.Value <= 0)
            {
                await _next(context);
                return;
            }

            var utcNow = DateTime.UtcNow;

            var monthStartUtc = new DateTime(
                utcNow.Year,
                utcNow.Month,
                1,
                0,
                0,
                0,
                DateTimeKind.Utc);

            var resetAtUtc = monthStartUtc.AddMonths(1);

            var requestsUsed = await db.ApiUsageLogs
                .AsNoTracking()
                .CountAsync(
                    x =>
                        x.ApiClientId == clientId.Value &&
                        x.CreatedAt >= monthStartUtc &&
                        x.CreatedAt < resetAtUtc,
                    context.RequestAborted);

            SetQuotaHeaders(
                context,
                monthlyQuota.Value,
                requestsUsed,
                resetAtUtc);

            if (requestsUsed < monthlyQuota.Value)
            {
                await _next(context);
                return;
            }

            _logger.LogWarning(
                "Monthly API quota exceeded. ClientId: {ClientId}, Used: {Used}, Quota: {Quota}, TraceId: {TraceId}",
                clientId.Value,
                requestsUsed,
                monthlyQuota.Value,
                context.TraceIdentifier);

            context.Response.Clear();

            context.Response.StatusCode =
                StatusCodes.Status429TooManyRequests;

            context.Response.ContentType =
                "application/json; charset=utf-8";

            context.Response.Headers.CacheControl = "no-store";

            SetQuotaHeaders(
                context,
                monthlyQuota.Value,
                requestsUsed,
                resetAtUtc);

            var response =
                ApiResponseFactory.Error<object>(
                    ApiErrorCodes.MonthlyQuotaExceeded,
                    "The monthly API request quota has been exceeded.",
                    context.TraceIdentifier,
                    meta: new
                    {
                        monthlyQuota = monthlyQuota.Value,
                        requestsUsed,
                        remaining = 0,
                        resetAtUtc
                    });

            await context.Response.WriteAsJsonAsync(
                response,
                context.RequestAborted);
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

            return int.TryParse(
                value?.ToString(),
                out var parsedValue)
                    ? parsedValue
                    : null;
        }

        private static void SetQuotaHeaders(
            HttpContext context,
            int quota,
            int used,
            DateTime resetAtUtc)
        {
            context.Response.Headers["X-Monthly-Quota-Limit"] =
                quota.ToString();

            context.Response.Headers["X-Monthly-Quota-Used"] =
                used.ToString();

            context.Response.Headers["X-Monthly-Quota-Remaining"] =
                Math.Max(quota - used, 0).ToString();

            context.Response.Headers["X-Monthly-Quota-Reset"] =
                resetAtUtc.ToString("O");
        }
    }
}