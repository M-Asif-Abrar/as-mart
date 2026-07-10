using AsMart.Web.Services;

namespace AsMart.Web.Middleware
{
    public class ApiKeyMiddleware
    {
        public const string ApiClientIdItem = "ApiClientId";
        public const string ApiClientNameItem = "ApiClientName";
        public const string ApiRateLimitItem = "ApiRateLimit";
        public const string ApiMonthlyQuotaItem = "ApiMonthlyQuota";
        public const string ApiUserIdItem = "ApiUserId";

        private const string ApiKeyHeaderName = "X-API-Key";

        private readonly RequestDelegate _next;

        public ApiKeyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            IApiKeyService apiKeyService)
        {
            if (!context.Request.Path.StartsWithSegments("/api"))
            {
                await _next(context);
                return;
            }

            var apiKey = context.Request.Headers[ApiKeyHeaderName]
                .FirstOrDefault();

            var client = await apiKeyService.GetClientAsync(apiKey);

            if (client != null)
            {
                context.Items[ApiClientIdItem] = client.Id;
                context.Items[ApiClientNameItem] = client.Name;
                context.Items[ApiRateLimitItem] = client.RateLimitPerMinute;
                context.Items[ApiMonthlyQuotaItem] = client.MonthlyQuota;

                if (!string.IsNullOrWhiteSpace(client.UserId))
                {
                    context.Items[ApiUserIdItem] = client.UserId;
                }

                await apiKeyService.UpdateLastUsedAsync(client.Id);
            }

            await _next(context);
        }
    }
}