using AsMart.Web.Models.Api;
using AsMart.Web.Services;

namespace AsMart.Web.Middleware
{
    public sealed class ApiKeyMiddleware
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

            var hasApiKeyHeader = context.Request.Headers
                .TryGetValue(ApiKeyHeaderName, out var headerValues);

            /*
             * Missing API key means anonymous/public API access.
             * Public requests remain subject to the anonymous rate limit.
             */
            if (!hasApiKeyHeader)
            {
                await _next(context);
                return;
            }

            var apiKey = headerValues.FirstOrDefault()?.Trim();

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                await WriteUnauthorizedResponseAsync(
                    context,
                    ApiErrorCodes.InvalidApiKey,
                    "The X-API-Key header was provided but is empty.");

                return;
            }

            var result = await apiKeyService.ValidateApiKeyAsync(
                apiKey,
                context.RequestAborted);

            if (!result.IsValid || result.Client is null)
            {
                await WriteValidationFailureAsync(
                    context,
                    result.Status);

                return;
            }

            var client = result.Client;

            context.Items[ApiClientIdItem] = client.Id;
            context.Items[ApiClientNameItem] = client.Name;
            context.Items[ApiRateLimitItem] =
                client.RateLimitPerMinute;
            context.Items[ApiMonthlyQuotaItem] =
                client.MonthlyQuota;

            if (!string.IsNullOrWhiteSpace(client.UserId))
            {
                context.Items[ApiUserIdItem] = client.UserId;
            }

            await apiKeyService.UpdateLastUsedAsync(
                client.Id,
                context.RequestAborted);

            await _next(context);
        }

        private static Task WriteValidationFailureAsync(
            HttpContext context,
            ApiKeyValidationStatus status)
        {
            return status switch
            {
                ApiKeyValidationStatus.Disabled =>
                    WriteUnauthorizedResponseAsync(
                        context,
                        ApiErrorCodes.ApiKeyDisabled,
                        "The supplied API key is disabled."),

                ApiKeyValidationStatus.Expired =>
                    WriteUnauthorizedResponseAsync(
                        context,
                        ApiErrorCodes.ApiKeyExpired,
                        "The supplied API key has expired."),

                ApiKeyValidationStatus.Revoked =>
                    WriteUnauthorizedResponseAsync(
                        context,
                        ApiErrorCodes.ApiKeyRevoked,
                        "The supplied API key has been revoked."),

                _ => WriteUnauthorizedResponseAsync(
                    context,
                    ApiErrorCodes.InvalidApiKey,
                    "The supplied API key is invalid.")
            };
        }

        private static async Task WriteUnauthorizedResponseAsync(
            HttpContext context,
            string errorCode,
            string message)
        {
            context.Response.Clear();

            context.Response.StatusCode =
                StatusCodes.Status401Unauthorized;

            context.Response.ContentType =
                "application/json; charset=utf-8";

            context.Response.Headers.CacheControl = "no-store";

            var response = ApiResponseFactory.Error(
                errorCode,
                message,
                context.TraceIdentifier);

            await context.Response.WriteAsJsonAsync(
                response,
                context.RequestAborted);
        }
    }
}