using Npgsql;
using Squadron;

namespace Mocha.Sagas.Tests;

/// <summary>
/// PostgreSQL test resource using Squadron
/// </summary>
public sealed class ExtendedPostgresResource : IAsyncLifetime
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

    public async Task CreateDatabaseAsync(string dbName)
    {
        await _resource.CreateDatabaseAsync(dbName);
    }

    public string GetConnectionString(string dbName)
    {
        return _resource.GetConnectionString(dbName);
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
