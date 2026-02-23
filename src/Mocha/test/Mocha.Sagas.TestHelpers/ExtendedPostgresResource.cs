using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Mocha.Sagas.Tests;

/// <summary>
/// PostgreSQL test resource using Testcontainers
/// </summary>
public sealed class ExtendedPostgresResource : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;

    public ExtendedPostgresResource()
    {
        _container = new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();
    }

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public async Task CreateDatabaseAsync(string dbName)
    {
        var connectionString = _container.GetConnectionString();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE \"{dbName}\"";
        await command.ExecuteNonQueryAsync();
    }

    public string GetConnectionString(string dbName)
    {
        var builder = new NpgsqlConnectionStringBuilder(_container.GetConnectionString()) { Database = dbName };
        return builder.ToString();
    }

    public async Task<string> FetchSchemaAsync(string dbName)
    {
        var connectionString = GetConnectionString(dbName);
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText =
            @"
            SELECT table_name, column_name, data_type, is_nullable
            FROM information_schema.columns
            WHERE table_schema = 'public'
            ORDER BY table_name, ordinal_position";

        await using var reader = await command.ExecuteReaderAsync();
        var schema = new System.Text.StringBuilder();

        while (await reader.ReadAsync())
        {
            schema.AppendLine(
                $"{reader["table_name"]}.{reader["column_name"]}: {reader["data_type"]} ({reader["is_nullable"]})");
        }

        return schema.ToString();
    }
}
