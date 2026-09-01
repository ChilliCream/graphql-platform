using Npgsql;
using Testcontainers.PostgreSql;

namespace CookieCrumble.Resources;

public class PostgreSqlResource : ContainerResource<PostgreSqlContainer>
{
    public string ConnectionString => Container.GetConnectionString();

    public async Task CreateDatabaseAsync(string name)
    {
        await using var connection = GetConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        var quotedName = new NpgsqlCommandBuilder().QuoteIdentifier(name);
        command.CommandText = $"CREATE DATABASE {quotedName};";
        await command.ExecuteNonQueryAsync();
    }

    public NpgsqlConnection GetConnection(string? database = null)
        => new(database is null ? ConnectionString : GetConnectionString(database));

    public string GetConnectionString(string database)
        => new NpgsqlConnectionStringBuilder(ConnectionString) { Database = database }
            .ConnectionString;

    public async Task RunSqlScriptAsync(string sql, string database)
    {
        await using var connection = GetConnection(database);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    protected override PostgreSqlContainer Build()
        => Configure(new PostgreSqlBuilder("postgres:15.1")).Build();

    protected virtual PostgreSqlBuilder Configure(PostgreSqlBuilder builder) => builder;
}
