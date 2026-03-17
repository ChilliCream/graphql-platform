using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Npgsql;
using Squadron;

namespace Mocha.Transport.Postgres.Tests.Helpers;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlResource _resource = new();

    public async Task InitializeAsync()
    {
        await _resource.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await _resource.DisposeAsync();
    }

    public string ConnectionString => _resource.ConnectionString;

    /// <summary>
    /// Creates an isolated database for each test to avoid interference.
    /// </summary>
    public async Task<DatabaseContext> CreateDatabaseAsync(
        [CallerMemberName] string testName = "",
        [CallerFilePath] string filePath = "")
    {
        var dbName = GenerateDatabaseName(testName, filePath);

        // Create the database
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"CREATE DATABASE \"{dbName}\"";
        await cmd.ExecuteNonQueryAsync();

        // Build connection string for the new database
        var builder = new NpgsqlConnectionStringBuilder(ConnectionString) { Database = dbName };
        return new DatabaseContext(this, dbName, builder.ConnectionString);
    }

    internal async Task DropDatabaseAsync(string dbName)
    {
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();
            // Terminate existing connections
            await using var termCmd = conn.CreateCommand();
            termCmd.CommandText = $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{dbName}' AND pid <> pg_backend_pid()";
            await termCmd.ExecuteNonQueryAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"DROP DATABASE IF EXISTS \"{dbName}\"";
            await cmd.ExecuteNonQueryAsync();
        }
        catch
        {
            // Best effort cleanup
        }
    }

    private static string GenerateDatabaseName(string testName, string filePath)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(filePath)))[..8];
        return $"mocha_{testName}_{hash}".ToLowerInvariant();
    }
}

public sealed class DatabaseContext(PostgresFixture fixture, string databaseName, string connectionString) : IAsyncDisposable
{
    public string ConnectionString => connectionString;
    public string DatabaseName => databaseName;

    public async ValueTask DisposeAsync() => await fixture.DropDatabaseAsync(databaseName);
}

[CollectionDefinition("Postgres")]
public class PostgresCollection : ICollectionFixture<PostgresFixture>;
