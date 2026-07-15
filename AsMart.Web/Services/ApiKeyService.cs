using System.Security.Cryptography;
using System.Text;
using AsMart.Web.Data;
using AsMart.Web.Models.Api;
using AsMart.Web.Models.Entities;
using AsMart.Web.Models.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AsMart.Web.Services
{
    public interface IApiKeyService
    {
        Task<ApiKeyValidationResult> ValidateApiKeyAsync(
            string? apiKey,
            CancellationToken cancellationToken = default);

        Task<ApiClient?> GetClientAsync(
            string? apiKey,
            CancellationToken cancellationToken = default);

        ApiKeyMaterial GenerateApiKeyMaterial();

        string ComputeHash(string rawApiKey);

        string CreatePrefix(string rawApiKey);

        Task UpdateLastUsedAsync(
            int clientId,
            CancellationToken cancellationToken = default);
    }

    public sealed class ApiKeyService : IApiKeyService
    {
        private const int PrefixLength = 18;
        private const int RequiredPepperLength = 32;

        private readonly ApplicationDbContext _db;
        private readonly byte[] _pepperBytes;

        public ApiKeyService(
            ApplicationDbContext db,
            IOptions<ApiKeySecurityOptions> options)
        {
            _db = db;

            ArgumentNullException.ThrowIfNull(options);

            var pepper = options.Value.HashingPepper?.Trim();

            if (string.IsNullOrWhiteSpace(pepper) ||
                pepper.Length < RequiredPepperLength)
            {
                throw new InvalidOperationException(
                    $"ApiKeySecurity:HashingPepper must contain at least {RequiredPepperLength} characters.");
            }

            _pepperBytes = Encoding.UTF8.GetBytes(pepper);
        }

        public async Task<ApiKeyValidationResult> ValidateApiKeyAsync(
            string? apiKey,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return ApiKeyValidationResult.Failed(
                    ApiKeyValidationStatus.Invalid);
            }

            var normalizedApiKey = apiKey.Trim();
            var apiKeyHash = ComputeHash(normalizedApiKey);

            /*
             * Phase 2:
             * API keys are validated only through ApiKeyHash.
             * No plaintext lookup or legacy backfill remains.
             */
            var client = await _db.ApiClients
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.ApiKeyHash == apiKeyHash,
                    cancellationToken);

            if (client is null)
            {
                return ApiKeyValidationResult.Failed(
                    ApiKeyValidationStatus.Invalid);
            }

            if (client.RevokedAt.HasValue)
            {
                return ApiKeyValidationResult.Failed(
                    ApiKeyValidationStatus.Revoked);
            }

            if (client.ExpiresAt.HasValue &&
                client.ExpiresAt.Value <= DateTime.UtcNow)
            {
                return ApiKeyValidationResult.Failed(
                    ApiKeyValidationStatus.Expired);
            }

            if (!client.IsActive)
            {
                return ApiKeyValidationResult.Failed(
                    ApiKeyValidationStatus.Disabled);
            }

            return ApiKeyValidationResult.Valid(client);
        }

        public async Task<ApiClient?> GetClientAsync(
            string? apiKey,
            CancellationToken cancellationToken = default)
        {
            var result = await ValidateApiKeyAsync(
                apiKey,
                cancellationToken);

            return result.Client;
        }

        public ApiKeyMaterial GenerateApiKeyMaterial()
        {
            /*
             * Generates 256 bits of cryptographically secure random data.
             */
            var randomBytes =
                RandomNumberGenerator.GetBytes(32);

            var encoded = Convert
                .ToBase64String(randomBytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

            var rawApiKey = $"asmart_{encoded}";

            return new ApiKeyMaterial(
                RawKey: rawApiKey,
                Hash: ComputeHash(rawApiKey),
                Prefix: CreatePrefix(rawApiKey));
        }

        public string ComputeHash(string rawApiKey)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(
                rawApiKey);

            var normalizedApiKey = rawApiKey.Trim();

            using var hmac =
                new HMACSHA256(_pepperBytes);

            var hashBytes = hmac.ComputeHash(
                Encoding.UTF8.GetBytes(normalizedApiKey));

            /*
             * HMAC-SHA256 produces 32 bytes.
             * Hex encoding produces exactly 64 characters.
             */
            return Convert.ToHexString(hashBytes);
        }

        public string CreatePrefix(string rawApiKey)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(
                rawApiKey);

            var normalizedApiKey = rawApiKey.Trim();

            return normalizedApiKey.Length <= PrefixLength
                ? normalizedApiKey
                : normalizedApiKey[..PrefixLength];
        }

        public async Task UpdateLastUsedAsync(
            int clientId,
            CancellationToken cancellationToken = default)
        {
            var utcNow = DateTime.UtcNow;
            var updateThreshold =
                utcNow.AddMinutes(-5);

            await _db.ApiClients
                .Where(x =>
                    x.Id == clientId &&
                    (
                        x.LastUsedAt == null ||
                        x.LastUsedAt < updateThreshold
                    ))
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        x => x.LastUsedAt,
                        utcNow),
                    cancellationToken);
        }
    }
}