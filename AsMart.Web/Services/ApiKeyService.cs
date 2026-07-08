using System.Security.Cryptography;
using AsMart.Web.Data;
using AsMart.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AsMart.Web.Services
{
    public interface IApiKeyService
    {
        Task<ApiClient?> GetClientAsync(string? apiKey);
        string GenerateApiKey();
        string MaskApiKey(string apiKey);
        Task UpdateLastUsedAsync(int clientId);
    }

    public class ApiKeyService : IApiKeyService
    {
        private readonly ApplicationDbContext _db;

        public ApiKeyService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<ApiClient?> GetClientAsync(string? apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                return null;

            return await _db.ApiClients
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ApiKey == apiKey && x.IsActive);
        }

        public string GenerateApiKey()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return $"asmart_{Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "")}";
        }

        public string MaskApiKey(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Length < 14)
                return "********";

            return $"{apiKey[..10]}********************************{apiKey[^6..]}";
        }

        public async Task UpdateLastUsedAsync(int clientId)
        {
            var client = await _db.ApiClients.FirstOrDefaultAsync(x => x.Id == clientId);

            if (client == null)
                return;

            client.LastUsedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }
}