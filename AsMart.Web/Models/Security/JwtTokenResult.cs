namespace AsMart.Web.Models.Security
{
    public sealed record JwtTokenResult(
        string AccessToken,
        DateTime ExpiresAtUtc);
}