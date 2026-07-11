using Microsoft.Extensions.Logging;
using Npgsql;

namespace Mocha.Transport.Postgres;

/// <summary>
/// Manages PostgreSQL connections for the messaging transport, providing connection pooling
/// via <see cref="NpgsqlDataSource"/> and database schema migration on first use.
/// </summary>
public sealed class PostgresConnectionManager : IAsyncDisposable
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly IReadOnlyPostgresSchemaOptions _schemaOptions;
    private readonly ILogger<PostgresConnectionManager> _logger;
    private bool _isMigrated;

    /// <summary>
    /// Creates a new connection manager with the specified connection string and schema options.
    /// </summary>
    public PostgresConnectionManager(
        string connectionString,
        PostgresSchemaOptions schemaOptions,
        ILogger<PostgresConnectionManager> logger)
    {
        _schemaOptions = schemaOptions;
        _logger = logger;

        var builder = new NpgsqlConnectionStringBuilder(connectionString) { Enlist = false, KeepAlive = 30 };

        _dataSource = new NpgsqlDataSourceBuilder(builder.ToString()).Build();
    }

    /// <summary>
    /// Opens a new connection from the connection pool.
    /// </summary>
    public async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        return await _dataSource.OpenConnectionAsync(cancellationToken);
    }

    /// <summary>
    /// Ensures the database schema is initialized, running migrations if needed.
    /// Uses an advisory lock to prevent concurrent migration attempts.
    /// </summary>
    public async Task EnsureMigratedAsync(CancellationToken cancellationToken)
    {
        if (_isMigrated)
        {
            return;
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        var migrator = new PostgresSchemaMigrator(_schemaOptions);
        await migrator.MigrateAsync(connection);
        _isMigrated = true;

        _logger.SchemaMigrated();
    }

    /// <summary>
    /// Checks whether the database is reachable by executing a lightweight <c>SELECT 1</c> query
    /// with a short timeout. Can be used by receive endpoints to verify connectivity before polling.
    /// </summary>
    /// <returns><c>true</c> if the database responded within 5 seconds; <c>false</c> otherwise.</returns>
    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            await using var connection = await _dataSource.OpenConnectionAsync(cts.Token);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandTimeout = 5;

            await command.ExecuteScalarAsync(cts.Token);

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _dataSource.DisposeAsync();
    }
}

internal static partial class Logs
{
    [LoggerMessage(LogLevel.Information, "PostgreSQL messaging schema migrated successfully.")]
    public static partial void SchemaMigrated(this ILogger logger);
}
