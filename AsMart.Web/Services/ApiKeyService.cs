using System.Security.Cryptography;
using AsMart.Web.Data;
using AsMart.Web.Models.Api;
using AsMart.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

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

        string GenerateApiKey();

        string MaskApiKey(string apiKey);

        Task UpdateLastUsedAsync(
            int clientId,
            CancellationToken cancellationToken = default);
    }

    public sealed class ApiKeyService : IApiKeyService
    {
        private readonly ApplicationDbContext _db;

        public ApiKeyService(ApplicationDbContext db)
        {
            _db = db;
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

            var client = await _db.ApiClients
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.ApiKey == normalizedApiKey,
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

        /*
         * Kept temporarily for backward compatibility with any existing code.
         * New middleware should use ValidateApiKeyAsync().
         */
        public async Task<ApiClient?> GetClientAsync(
            string? apiKey,
            CancellationToken cancellationToken = default)
        {
            var result = await ValidateApiKeyAsync(
                apiKey,
                cancellationToken);

            return result.Client;
        }

        public string GenerateApiKey()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);

            var encoded = Convert
                .ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

            return $"asmart_{encoded}";
        }

        public string MaskApiKey(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey) ||
                apiKey.Length < 16)
            {
                return "********";
            }

            return
                $"{apiKey[..10]}********************************{apiKey[^6..]}";
        }

        public async Task UpdateLastUsedAsync(
            int clientId,
            CancellationToken cancellationToken = default)
        {
            var utcNow = DateTime.UtcNow;
            var updateThreshold = utcNow.AddMinutes(-5);

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