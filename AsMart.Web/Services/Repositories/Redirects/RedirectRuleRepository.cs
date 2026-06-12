// /Redirects/RedirectRuleRepository.cs
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace AsMart.Web.Services.Repositories.Redirects
{
    public sealed class RedirectRuleRepository
    {
        private readonly IConfiguration _config;

        public RedirectRuleRepository(IConfiguration config)
        {
            _config = config;
        }

        public async Task<RedirectRule?> FindAsync(string fromPath)
        {
            var cs = _config.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(cs)) return null;

            var normalized = NormalizePath(fromPath);

            await using var con = new SqlConnection(cs);
            await con.OpenAsync();

            await using var cmd = con.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = @"
                SELECT TOP 1 Id, FromPath, ToUrl, StatusCode
                FROM dbo.RedirectRules
                WHERE IsEnabled = 1 AND FromPath = @FromPath;";

            cmd.Parameters.Add(new SqlParameter("@FromPath", SqlDbType.NVarChar, 512) { Value = normalized });

            await using var r = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow);
            if (!await r.ReadAsync()) return null;

            return new RedirectRule
            {
                Id = r.GetInt64(0),
                FromPath = r.GetString(1),
                ToUrl = r.GetString(2),
                StatusCode = r.GetInt32(3)
            };
        }

        public async Task TrackHitAsync(long id)
        {
            var cs = _config.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(cs)) return;

            await using var con = new SqlConnection(cs);
            await con.OpenAsync();

            await using var cmd = con.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = @"
                UPDATE dbo.RedirectRules
                SET HitCount = HitCount + 1,
                    LastHitAtUtc = SYSUTCDATETIME(),
                    UpdatedAtUtc = SYSUTCDATETIME()
                WHERE Id = @Id;";

            cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.BigInt) { Value = id });
            await cmd.ExecuteNonQueryAsync();
        }

        public static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return "/";

            var p = path.Trim();
            if (!p.StartsWith("/")) p = "/" + p;

            var q = p.IndexOf('?');
            if (q >= 0) p = p.Substring(0, q);

            if (p.Length > 1 && p.EndsWith("/")) p = p.TrimEnd('/');

            return p;
        }
    }

    public sealed class RedirectRule
    {
        public long Id { get; set; }
        public string FromPath { get; set; } = "";
        public string ToUrl { get; set; } = "";
        public int StatusCode { get; set; } = 301;
    }
}
