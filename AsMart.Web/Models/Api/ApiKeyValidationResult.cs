using AsMart.Web.Models.Entities;

namespace AsMart.Web.Models.Api
{
    public sealed class ApiKeyValidationResult
    {
        public ApiKeyValidationStatus Status { get; init; }

        public ApiClient? Client { get; init; }

        public bool IsValid =>
            Status == ApiKeyValidationStatus.Valid &&
            Client is not null;

        public static ApiKeyValidationResult Valid(ApiClient client)
        {
            return new ApiKeyValidationResult
            {
                Status = ApiKeyValidationStatus.Valid,
                Client = client
            };
        }

        public static ApiKeyValidationResult Failed(
            ApiKeyValidationStatus status)
        {
            return new ApiKeyValidationResult
            {
                Status = status
            };
        }
    }
}