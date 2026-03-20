using Npgsql;

namespace Mocha.Transport.Postgres;

/// <summary>
/// Manages database schema migrations for the PostgreSQL messaging transport.
/// Uses PostgreSQL advisory locks to prevent concurrent migration attempts.
/// </summary>
internal sealed class PostgresSchemaMigrator
{
    private const int LockId = 958913715;

    private readonly IReadOnlyPostgresSchemaOptions _settings;

    public PostgresSchemaMigrator(IReadOnlyPostgresSchemaOptions settings)
    {
        _settings = settings;
    }

    public async Task MigrateAsync(NpgsqlConnection connection)
    {
        await using var transaction = await connection.BeginTransactionAsync(CancellationToken.None);

        await AcquireLockAsync(connection);
        await EnsureMigrationsTableAsync(connection, _settings);

        var migrations = new List<MigrationInfo>
        {
            new("2026-03-06_InitialSchema", PostgresSchemaSql.InitialSchema(_settings)),
            new("2026-03-06_AddTransportIndex", PostgresSchemaSql.AddTransportIndex(_settings)),
            new("2026-03-06_AddConsumerManagement", PostgresSchemaSql.AddConsumerManagement(_settings))
        };

        var applied = await GetAppliedMigrationsAsync(connection, _settings);

        foreach (var migration in migrations)
        {
            if (!applied.Contains(migration.Id))
            {
                await ApplyMigrationAsync(connection, _settings, migration);
            }
        }

        await transaction.CommitAsync();
    }

    private static async Task AcquireLockAsync(NpgsqlConnection connection)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT pg_advisory_xact_lock(@lockId);";
        cmd.Parameters.AddWithValue("lockId", LockId);

        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task EnsureMigrationsTableAsync(
        NpgsqlConnection connection,
        IReadOnlyPostgresSchemaOptions settings)
    {
        await using var cmd = connection.CreateCommand();

        cmd.CommandText = $"""
            CREATE SCHEMA IF NOT EXISTS {settings.Schema};

            CREATE TABLE IF NOT EXISTS {settings.MigrationsTable}
            (
                migration_id  text        NOT NULL PRIMARY KEY,
                applied_on    timestamptz NOT NULL DEFAULT (now() at time zone 'utc')
            );
            """;

        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<HashSet<string>> GetAppliedMigrationsAsync(
        NpgsqlConnection connection,
        IReadOnlyPostgresSchemaOptions settings)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT migration_id FROM {settings.MigrationsTable};";

        await using var reader = await cmd.ExecuteReaderAsync();
        var applied = new HashSet<string>();
        while (await reader.ReadAsync())
        {
            applied.Add(reader.GetString(0));
        }

        return applied;
    }

    private static async Task ApplyMigrationAsync(
        NpgsqlConnection connection,
        IReadOnlyPostgresSchemaOptions settings,
        MigrationInfo migration)
    {
        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = migration.Sql;
            await cmd.ExecuteNonQueryAsync();
        }

        await using (var insertCmd = connection.CreateCommand())
        {
            insertCmd.CommandText = $"""
                INSERT INTO {settings.MigrationsTable} (migration_id)
                VALUES (@id);
                """;
            insertCmd.Parameters.AddWithValue("id", migration.Id);
            await insertCmd.ExecuteNonQueryAsync();
        }
    }

    private record MigrationInfo(string Id, string Sql);
}
