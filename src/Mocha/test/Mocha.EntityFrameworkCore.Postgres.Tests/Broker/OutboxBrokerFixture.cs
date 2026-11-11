using CookieCrumble.Resources;
using Npgsql;
using RabbitMQ.Client;

namespace Mocha.EntityFrameworkCore.Postgres.Tests.Broker;

/// <summary>
/// Runs a Postgres server and a RabbitMQ broker that only the tests of one class use, so a test can
/// block or stop the broker.
/// </summary>
public sealed class OutboxBrokerFixture : IAsyncLifetime
{
    private readonly PostgreSqlResource _postgres = new();
    private readonly RabbitMQResource _broker = new();

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_postgres.InitializeAsync().AsTask(), _broker.InitializeAsync().AsTask());
    }

    public async ValueTask DisposeAsync()
    {
        await _broker.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    public async Task<string> CreateDatabaseAsync()
    {
        var database = $"test_{Guid.NewGuid():N}";
        await _postgres.CreateDatabaseAsync(database);
        return _postgres.GetConnectionString(database);
    }

    /// <summary>
    /// Creates a virtual host and returns the factory of the connections to it.
    /// </summary>
    public async Task<IConnectionFactory> CreateVirtualHostAsync()
    {
        var virtualHost = $"test_{Guid.NewGuid():N}";
        await _broker.InvokeCommandAsync(["rabbitmqctl", "add_vhost", virtualHost]);
        await _broker.InvokeCommandAsync(
            ["rabbitmqctl", "set_permissions", "-p", virtualHost, "guest", ".*", ".*", ".*"]);

        return new ConnectionFactory { Uri = new Uri(_broker.ConnectionString), VirtualHost = virtualHost };
    }

    /// <summary>
    /// Raises the memory alarm, so the broker stops reading from connections that publish until the returned
    /// handle is disposed.
    /// </summary>
    public async Task<IAsyncDisposable> BlockPublishersAsync()
    {
        await _broker.InvokeCommandAsync(["rabbitmqctl", "set_vm_memory_high_watermark", "0"]);

        return new Restore(() => _broker.InvokeCommandAsync(["rabbitmqctl", "set_vm_memory_high_watermark", "0.4"]));
    }

    /// <summary>
    /// Stops the broker application, which closes every connection and refuses new ones until the returned
    /// handle is disposed.
    /// </summary>
    public async Task<IAsyncDisposable> StopBrokerAsync()
    {
        await _broker.InvokeCommandAsync(["rabbitmqctl", "stop_app"]);

        return new Restore(async () =>
        {
            await _broker.InvokeCommandAsync(["rabbitmqctl", "start_app"]);
            await _broker.InvokeCommandAsync(["rabbitmqctl", "await_startup"]);
        });
    }

    /// <summary>
    /// Waits until no other session of the database has an open transaction and returns the number of
    /// sessions that had one when the wait ended.
    /// </summary>
    public static async Task<long> WaitForNoOpenTransactionAsync(
        string connectionString,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + timeout;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT COUNT(*) FROM pg_stat_activity
            WHERE datname = current_database()
                AND backend_type = 'client backend'
                AND pid <> pg_backend_pid()
                AND xact_start IS NOT NULL;
            """;

        var sessions = (long)(await command.ExecuteScalarAsync(cancellationToken))!;

        while (sessions != 0 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20, cancellationToken);
            sessions = (long)(await command.ExecuteScalarAsync(cancellationToken))!;
        }

        return sessions;
    }

    private sealed class Restore(Func<Task> restore) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync() => await restore();
    }
}
