using NpgsqlTypes;

namespace Mocha.Transport.Postgres;

/// <summary>
/// Manages consumer lifecycle in the PostgreSQL messaging transport, including registration,
/// heartbeat updates, unregistration, and cleanup of expired consumers.
/// </summary>
public sealed class PostgresConsumerManager
{
    private readonly IReadOnlyPostgresSchemaOptions _schemaOptions;
    private readonly PostgresConnectionManager _connectionManager;
    private readonly string _serviceName;

    /// <summary>
    /// Gets the unique identifier for this consumer instance.
    /// </summary>
#if NET9_0_OR_GREATER
    public Guid ConsumerId { get; } = Guid.CreateVersion7();
#else
    public Guid ConsumerId { get; } = Guid.NewGuid();
#endif

    /// <summary>
    /// Creates a new consumer manager with the specified service name.
    /// </summary>
    /// <param name="serviceName">The name of the service this consumer belongs to.</param>
    /// <param name="connectionManager">The connection manager for database access.</param>
    /// <param name="schemaOptions">The schema options for the PostgreSQL transport.</param>
    public PostgresConsumerManager(
        string serviceName,
        PostgresConnectionManager connectionManager,
        IReadOnlyPostgresSchemaOptions schemaOptions)
    {
        _serviceName = serviceName;
        _connectionManager = connectionManager;
        _schemaOptions = schemaOptions;
    }

    /// <summary>
    /// Registers the consumer in the database by inserting a row into the consumers table.
    /// </summary>
    public async Task RegisterAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _connectionManager.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = $"""
            INSERT INTO {_schemaOptions.ConsumersTable} (id, service_name)
            VALUES (@id, @service_name)
            ON CONFLICT (id) DO NOTHING
            """;

        command.Parameters.Add(new("id", NpgsqlDbType.Uuid) { Value = ConsumerId });
        command.Parameters.Add(new("service_name", NpgsqlDbType.Text) { Value = _serviceName });

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Updates the consumer's heartbeat timestamp to indicate it is still active.
    /// Returns <c>false</c> if the consumer row no longer exists (evicted by another pod).
    /// </summary>
    public async Task<bool> HeartbeatAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _connectionManager.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = $"""
            UPDATE {_schemaOptions.ConsumersTable}
            SET updated_at = now()
            WHERE id = @id
            """;

        command.Parameters.Add(new("id", NpgsqlDbType.Uuid) { Value = ConsumerId });

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

        return rowsAffected > 0;
    }

    /// <summary>
    /// Unregisters the consumer by deleting its row from the consumers table.
    /// The CASCADE constraint will automatically clean up any temporary queues owned by this consumer.
    /// </summary>
    public async Task UnregisterAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _connectionManager.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = $"""
            DELETE FROM {_schemaOptions.ConsumersTable}
            WHERE id = @id
            """;

        command.Parameters.Add(new("id", NpgsqlDbType.Uuid) { Value = ConsumerId });

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Deletes consumers whose heartbeat has not been updated within the specified timeout,
    /// triggering CASCADE deletion of their temporary queues and messages.
    /// </summary>
    public async Task CleanupExpiredConsumersAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionManager.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = $"""
            DELETE FROM {_schemaOptions.ConsumersTable}
            WHERE updated_at < now() - @timeout
            """;

        command.Parameters.Add(new("timeout", NpgsqlDbType.Interval) { Value = timeout });

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
