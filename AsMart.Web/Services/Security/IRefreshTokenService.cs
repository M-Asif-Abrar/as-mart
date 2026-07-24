namespace AsMart.Web.Services.Security
{
    public interface IRefreshTokenService
    {
        Task<RefreshTokenIssueResult> CreateAsync(
            string userId,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken = default);

        Task<RefreshTokenRotationResult?> RotateAsync(
            string rawToken,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken = default);

        Task<bool> RevokeAsync(
            string rawToken,
            string userId,
            string? ipAddress,
            CancellationToken cancellationToken = default);
    }
}
