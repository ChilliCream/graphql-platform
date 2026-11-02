using Microsoft.Extensions.Logging;

namespace Mocha.Transport.Postgres.Tasks;

/// <summary>
/// A background task that periodically releases messages orphaned by evicted consumers.
/// Messages locked (consumer_id IS NOT NULL) for longer than 5 minutes are considered
/// orphaned and have their consumer_id cleared so they can be redelivered. Runs every 60 seconds.
/// </summary>
internal sealed class OrphanedMessageCleanupTask(
    PostgresConnectionManager connectionManager,
    IReadOnlyPostgresSchemaOptions schemaOptions,
    ILogger logger)
    : PostgresBackgroundTask(connectionManager, schemaOptions, logger)
{
    private static readonly TimeSpan s_lockTimeout = TimeSpan.FromMinutes(5);

    /// <inheritdoc />
    protected override TimeSpan Interval => TimeSpan.FromSeconds(60);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var naming = SchemaOptions;
        await using var connection = await ConnectionManager.OpenConnectionAsync(ct);
        await using var command = connection.CreateCommand();

        command.CommandText =
            $"""
            WITH released AS (
                UPDATE {naming.MessageTable}
                SET consumer_id = NULL
                WHERE consumer_id IS NOT NULL
                  AND last_delivered < now() AT TIME ZONE 'utc' - @lockTimeout
                RETURNING queue_id
            )
            SELECT pg_notify(
                '{naming.NotificationChannel}',
                q.name::text
            )
            FROM released
            JOIN {naming.QueueTable} q ON released.queue_id = q.id;
            """;

        command.Parameters.AddWithValue("lockTimeout", s_lockTimeout);

        await command.ExecuteNonQueryAsync(ct);
    }
}
