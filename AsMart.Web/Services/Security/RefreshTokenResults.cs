using AsMart.Web.Models.Entities;

namespace AsMart.Web.Services.Security
{
    public sealed record RefreshTokenIssueResult(
        string RawToken,
        DateTime ExpiresAtUtc);

    public sealed record RefreshTokenRotationResult(
        ApplicationUser User,
        string RawToken,
        DateTime ExpiresAtUtc);
}
