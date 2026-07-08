using AsMart.Web.Services;

namespace AsMart.Web.Middleware
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;

        public ApiKeyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IApiKeyService apiKeyService)
        {
            if (!context.Request.Path.StartsWithSegments("/api"))
            {
                await _next(context);
                return;
            }

            var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault();
            var client = await apiKeyService.GetClientAsync(apiKey);

            if (client != null)
            {
                context.Items["ApiClientId"] = client.Id;
                context.Items["ApiClientName"] = client.Name;
                context.Items["ApiRateLimit"] = client.RateLimitPerMinute;

                await apiKeyService.UpdateLastUsedAsync(client.Id);
            }

            await _next(context);
        }
    }
}