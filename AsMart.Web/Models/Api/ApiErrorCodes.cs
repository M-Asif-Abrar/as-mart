namespace AsMart.Web.Models.Api
{
    public static class ApiErrorCodes
    {
        public const string InvalidApiKey = "invalid_api_key";
        public const string ApiKeyExpired = "api_key_expired";
        public const string ApiKeyRevoked = "api_key_revoked";
        public const string ApiKeyDisabled = "api_key_disabled";

        public const string ValidationFailed = "validation_failed";
        public const string InvalidRequest = "invalid_request";
        public const string NotFound = "not_found";

        public const string ProductNotFound = "product_not_found";
        public const string CategoryNotFound = "category_not_found";
        public const string CollectionNotFound = "collection_not_found";
        public const string BlogNotFound = "blog_not_found";
        public const string SeoPageNotFound = "seo_page_not_found";

        public const string RateLimitExceeded = "rate_limit_exceeded";
        public const string MonthlyQuotaExceeded = "monthly_quota_exceeded";

        public const string ServerError = "server_error";
        public const string ServiceUnavailable = "service_unavailable";
    }
}