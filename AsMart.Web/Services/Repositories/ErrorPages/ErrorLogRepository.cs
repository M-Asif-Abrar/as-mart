using Microsoft.Data.SqlClient;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace AsMart.Web.Services.Repositories.ErrorPages
{
    public sealed class ErrorLogRepository
    {
        private readonly IConfiguration _config;

        public ErrorLogRepository(IConfiguration config)
        {
            _config = config;
        }

        public async Task Log404Async(string path, string fullUrl, string? referrer, string? userAgent)
        {
            var cs = _config.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(cs)) return;

            var slug = ExtractSlug(path);

            await using var con = new SqlConnection(cs);
            await con.OpenAsync();

            await using var cmd = con.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = @"
                            INSERT INTO dbo.Error404Logs (Slug, Path, FullUrl, Referrer, UserAgent, CreatedAtUtc)
                            VALUES (@Slug, @Path, @FullUrl, @Referrer, @UserAgent, SYSUTCDATETIME());";

            cmd.Parameters.Add(new SqlParameter("@Slug", SqlDbType.NVarChar, 300) { Value = (object?)slug ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@Path", SqlDbType.NVarChar, 512) { Value = (object?)path ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@FullUrl", SqlDbType.NVarChar, 2048) { Value = (object?)fullUrl ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@Referrer", SqlDbType.NVarChar, 2048) { Value = (object?)referrer ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@UserAgent", SqlDbType.NVarChar, 512) { Value = (object?)userAgent ?? DBNull.Value });

            await cmd.ExecuteNonQueryAsync();
        }

        private static string? ExtractSlug(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;

            var p = path.Trim();
            if (p.EndsWith("/")) p = p.TrimEnd('/');

            var last = p.LastIndexOf('/');
            if (last < 0) return p;

            var seg = p[(last + 1)..];
            return string.IsNullOrWhiteSpace(seg) ? null : seg;
        }
    }
}
