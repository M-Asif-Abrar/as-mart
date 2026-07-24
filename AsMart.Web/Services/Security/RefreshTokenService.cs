using AsMart.Web.Data;
using AsMart.Web.Models.Entities;
using AsMart.Web.Models.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace AsMart.Web.Services.Security
{
    public sealed class RefreshTokenService : IRefreshTokenService
    {
        private readonly ApplicationDbContext _db;
        private readonly JwtOptions _options;
        private readonly TimeProvider _timeProvider;

        public RefreshTokenService(
            ApplicationDbContext db,
            IOptions<JwtOptions> options,
            TimeProvider timeProvider)
        {
            _db = db;
            _options = options.Value;
            _timeProvider = timeProvider;
        }

        public async Task<RefreshTokenIssueResult> CreateAsync(
            string userId,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken = default)
        {
            var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
            var expiresAtUtc = nowUtc.AddDays(
                _options.RefreshTokenDays);

            var rawToken = GenerateToken();
            var tokenHash = HashToken(rawToken);

            var entity = new RefreshToken
            {
                UserId = userId,
                TokenHash = tokenHash,
                CreatedAtUtc = nowUtc,
                ExpiresAtUtc = expiresAtUtc,
                CreatedByIp = Truncate(ipAddress, 64),
                UserAgent = Truncate(userAgent, 512)
            };

            _db.Set<RefreshToken>().Add(entity);
            await _db.SaveChangesAsync(cancellationToken);

            return new RefreshTokenIssueResult(
                rawToken,
                expiresAtUtc);
        }

        public async Task<RefreshTokenRotationResult?> RotateAsync(
            string rawToken,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(rawToken))
            {
                return null;
            }

            var tokenHash = HashToken(rawToken);
            var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

            await using var transaction =
                await _db.Database.BeginTransactionAsync(
                    cancellationToken);

            var current = await _db.Set<RefreshToken>()
                .Include(x => x.User)
                .SingleOrDefaultAsync(
                    x => x.TokenHash == tokenHash,
                    cancellationToken);

            if (current is null ||
                current.RevokedAtUtc is not null ||
                current.ExpiresAtUtc <= nowUtc)
            {
                return null;
            }

            var newRawToken = GenerateToken();
            var newHash = HashToken(newRawToken);
            var newExpiry = nowUtc.AddDays(
                _options.RefreshTokenDays);

            current.RevokedAtUtc = nowUtc;
            current.RevokedByIp = Truncate(ipAddress, 64);
            current.ReplacedByTokenHash = newHash;

            var replacement = new RefreshToken
            {
                UserId = current.UserId,
                TokenHash = newHash,
                CreatedAtUtc = nowUtc,
                ExpiresAtUtc = newExpiry,
                CreatedByIp = Truncate(ipAddress, 64),
                UserAgent = Truncate(userAgent, 512)
            };

            _db.Set<RefreshToken>().Add(replacement);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new RefreshTokenRotationResult(
                current.User,
                newRawToken,
                newExpiry);
        }

        public async Task<bool> RevokeAsync(
            string rawToken,
            string userId,
            string? ipAddress,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(rawToken) ||
                string.IsNullOrWhiteSpace(userId))
            {
                return false;
            }

            var tokenHash = HashToken(rawToken);
            var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

            var token = await _db.Set<RefreshToken>()
                .SingleOrDefaultAsync(
                    x => x.TokenHash == tokenHash &&
                         x.UserId == userId,
                    cancellationToken);

            if (token is null ||
                token.RevokedAtUtc is not null)
            {
                return false;
            }

            token.RevokedAtUtc = nowUtc;
            token.RevokedByIp = Truncate(ipAddress, 64);

            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        private static string GenerateToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);

            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static string HashToken(string rawToken)
        {
            var bytes = SHA256.HashData(
                Encoding.UTF8.GetBytes(rawToken));

            return Convert.ToHexString(bytes);
        }

        private static string? Truncate(
            string? value,
            int maximumLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            value = value.Trim();

            return value.Length <= maximumLength
                ? value
                : value[..maximumLength];
        }
    }
}
