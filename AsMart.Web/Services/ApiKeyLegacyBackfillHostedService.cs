using System.Data;
using System.Data.Common;
using AsMart.Web.Data;
using AsMart.Web.Models.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AsMart.Web.Services
{
    public sealed class ApiKeyLegacyBackfillHostedService : IHostedService
    {
        private const string SchemaName = "dbo";
        private const string TableName = "ApiClients";
        private const string LegacyApiKeyColumnName = "ApiKey";

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptions<ApiKeySecurityOptions> _options;
        private readonly ILogger<ApiKeyLegacyBackfillHostedService> _logger;

        public ApiKeyLegacyBackfillHostedService(
            IServiceScopeFactory scopeFactory,
            IOptions<ApiKeySecurityOptions> options,
            ILogger<ApiKeyLegacyBackfillHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options;
            _logger = logger;
        }

        public async Task StartAsync(
            CancellationToken cancellationToken)
        {
            var securityOptions = _options.Value;

            if (!securityOptions.EnableLegacyBackfill)
            {
                _logger.LogInformation(
                    "Legacy API-key backfill is disabled.");

                return;
            }

            _logger.LogWarning(
                "Legacy API-key backfill is enabled. " +
                "This setting must be disabled after the migration is verified.");

            try
            {
                using var scope = _scopeFactory.CreateScope();

                var dbContext =
                    scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var apiKeyService =
                    scope.ServiceProvider.GetRequiredService<IApiKeyService>();

                var connection = dbContext.Database.GetDbConnection();

                await EnsureConnectionOpenAsync(
                    connection,
                    cancellationToken);

                var legacyColumnExists =
                    await LegacyColumnExistsAsync(
                        connection,
                        cancellationToken);

                if (!legacyColumnExists)
                {
                    _logger.LogInformation(
                        "Legacy column {Schema}.{Table}.{Column} does not exist. " +
                        "No API-key backfill is required.",
                        SchemaName,
                        TableName,
                        LegacyApiKeyColumnName);

                    return;
                }

                var legacyKeys = await ReadLegacyKeysAsync(
                    connection,
                    cancellationToken);

                if (legacyKeys.Count == 0)
                {
                    _logger.LogInformation(
                        "No API clients require legacy API-key backfill.");

                    return;
                }

                _logger.LogWarning(
                    "Found {Count} legacy API key(s) requiring backfill.",
                    legacyKeys.Count);

                await using var transaction =
                    await connection.BeginTransactionAsync(
                        cancellationToken);

                try
                {
                    var migratedCount = 0;

                    foreach (var legacyKey in legacyKeys)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var rawApiKey = legacyKey.RawApiKey.Trim();

                        if (string.IsNullOrWhiteSpace(rawApiKey))
                        {
                            _logger.LogWarning(
                                "Skipping API client {ApiClientId} because its legacy key is empty.",
                                legacyKey.ApiClientId);

                            continue;
                        }

                        var hash =
                            apiKeyService.ComputeHash(rawApiKey);

                        var prefix =
                            apiKeyService.CreatePrefix(rawApiKey);

                        var updated = await UpdateApiClientAsync(
                            connection,
                            transaction,
                            legacyKey.ApiClientId,
                            hash,
                            prefix,
                            securityOptions.ClearLegacyPlaintextAfterBackfill,
                            cancellationToken);

                        if (updated)
                        {
                            migratedCount++;
                        }
                    }

                    await transaction.CommitAsync(
                        cancellationToken);

                    _logger.LogWarning(
                        "Legacy API-key backfill completed successfully. " +
                        "{MigratedCount} of {TotalCount} API key(s) were migrated.",
                        migratedCount,
                        legacyKeys.Count);

                    if (!securityOptions.ClearLegacyPlaintextAfterBackfill)
                    {
                        _logger.LogWarning(
                            "Legacy plaintext API keys were not cleared because " +
                            "ClearLegacyPlaintextAfterBackfill is false.");
                    }

                    _logger.LogWarning(
                        "Set ApiKeySecurity:EnableLegacyBackfill to false now.");
                }
                catch
                {
                    await transaction.RollbackAsync(
                        cancellationToken);

                    throw;
                }
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "Legacy API-key backfill was cancelled.");

                throw;
            }
            catch (Exception exception)
            {
                _logger.LogCritical(
                    exception,
                    "Legacy API-key backfill failed.");

                /*
                 * Stop application startup rather than allowing the site to
                 * run with a partially completed security migration.
                 */
                throw;
            }
        }

        public Task StopAsync(
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private static async Task EnsureConnectionOpenAsync(
            DbConnection connection,
            CancellationToken cancellationToken)
        {
            if (connection.State == ConnectionState.Open)
            {
                return;
            }

            await connection.OpenAsync(cancellationToken);
        }

        private static async Task<bool> LegacyColumnExistsAsync(
            DbConnection connection,
            CancellationToken cancellationToken)
        {
            const string sql =
                """
                SELECT CASE
                    WHEN EXISTS
                    (
                        SELECT 1
                        FROM sys.columns AS c
                        INNER JOIN sys.tables AS t
                            ON t.object_id = c.object_id
                        INNER JOIN sys.schemas AS s
                            ON s.schema_id = t.schema_id
                        WHERE s.name = @SchemaName
                          AND t.name = @TableName
                          AND c.name = @ColumnName
                    )
                    THEN CAST(1 AS bit)
                    ELSE CAST(0 AS bit)
                END;
                """;

            await using var command = connection.CreateCommand();

            command.CommandText = sql;
            command.CommandType = CommandType.Text;

            AddParameter(
                command,
                "@SchemaName",
                SchemaName);

            AddParameter(
                command,
                "@TableName",
                TableName);

            AddParameter(
                command,
                "@ColumnName",
                LegacyApiKeyColumnName);

            var result = await command.ExecuteScalarAsync(
                cancellationToken);

            return result is true ||
                   result is bool boolResult && boolResult ||
                   result is not null &&
                   Convert.ToBoolean(result);
        }

        private static async Task<List<LegacyApiKeyRecord>>
            ReadLegacyKeysAsync(
                DbConnection connection,
                CancellationToken cancellationToken)
        {
            /*
             * The table and column identifiers are constants controlled by
             * the application. No user-provided values are inserted here.
             */
            const string sql =
                """
                SELECT
                    [Id],
                    [ApiKey]
                FROM [dbo].[ApiClients]
                WHERE [ApiKey] IS NOT NULL
                  AND LTRIM(RTRIM([ApiKey])) <> ''
                  AND
                  (
                      [ApiKeyHash] IS NULL
                      OR LTRIM(RTRIM([ApiKeyHash])) = ''
                      OR [ApiKeyPrefix] IS NULL
                      OR LTRIM(RTRIM([ApiKeyPrefix])) = ''
                  );
                """;

            var records =
                new List<LegacyApiKeyRecord>();

            await using var command = connection.CreateCommand();

            command.CommandText = sql;
            command.CommandType = CommandType.Text;

            await using var reader =
                await command.ExecuteReaderAsync(
                    cancellationToken);

            var idOrdinal =
                reader.GetOrdinal("Id");

            var apiKeyOrdinal =
                reader.GetOrdinal("ApiKey");

            while (await reader.ReadAsync(cancellationToken))
            {
                if (reader.IsDBNull(apiKeyOrdinal))
                {
                    continue;
                }

                var apiClientId =
                    reader.GetInt32(idOrdinal);

                var rawApiKey =
                    reader.GetString(apiKeyOrdinal);

                records.Add(
                    new LegacyApiKeyRecord(
                        apiClientId,
                        rawApiKey));
            }

            return records;
        }

        private static async Task<bool> UpdateApiClientAsync(
            DbConnection connection,
            DbTransaction transaction,
            int apiClientId,
            string apiKeyHash,
            string apiKeyPrefix,
            bool clearLegacyPlaintext,
            CancellationToken cancellationToken)
        {
            var sql = clearLegacyPlaintext
                ?
                """
                UPDATE [dbo].[ApiClients]
                SET
                    [ApiKeyHash] = @ApiKeyHash,
                    [ApiKeyPrefix] = @ApiKeyPrefix,
                    [ApiKey] = NULL
                WHERE [Id] = @ApiClientId
                  AND
                  (
                      [ApiKeyHash] IS NULL
                      OR LTRIM(RTRIM([ApiKeyHash])) = ''
                      OR [ApiKeyPrefix] IS NULL
                      OR LTRIM(RTRIM([ApiKeyPrefix])) = ''
                  );
                """
                :
                """
                UPDATE [dbo].[ApiClients]
                SET
                    [ApiKeyHash] = @ApiKeyHash,
                    [ApiKeyPrefix] = @ApiKeyPrefix
                WHERE [Id] = @ApiClientId
                  AND
                  (
                      [ApiKeyHash] IS NULL
                      OR LTRIM(RTRIM([ApiKeyHash])) = ''
                      OR [ApiKeyPrefix] IS NULL
                      OR LTRIM(RTRIM([ApiKeyPrefix])) = ''
                  );
                """;

            await using var command =
                connection.CreateCommand();

            command.Transaction = transaction;
            command.CommandText = sql;
            command.CommandType = CommandType.Text;

            AddParameter(
                command,
                "@ApiClientId",
                apiClientId);

            AddParameter(
                command,
                "@ApiKeyHash",
                apiKeyHash);

            AddParameter(
                command,
                "@ApiKeyPrefix",
                apiKeyPrefix);

            var affectedRows =
                await command.ExecuteNonQueryAsync(
                    cancellationToken);

            return affectedRows == 1;
        }

        private static void AddParameter(
            DbCommand command,
            string parameterName,
            object value)
        {
            var parameter =
                command.CreateParameter();

            parameter.ParameterName = parameterName;
            parameter.Value = value;

            command.Parameters.Add(parameter);
        }

        private sealed record LegacyApiKeyRecord(
            int ApiClientId,
            string RawApiKey);
    }
}