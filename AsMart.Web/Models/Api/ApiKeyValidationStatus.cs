namespace AsMart.Web.Models.Api
{
    public enum ApiKeyValidationStatus
    {
        Valid = 0,
        Invalid = 1,
        Disabled = 2,
        Expired = 3,
        Revoked = 4
    }
}