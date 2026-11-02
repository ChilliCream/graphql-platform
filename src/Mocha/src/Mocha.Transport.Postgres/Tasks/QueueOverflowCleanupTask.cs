using Microsoft.Extensions.Logging;
using NpgsqlTypes;

namespace Mocha.Transport.Postgres.Tasks;

/// <summary>
/// A background task that periodically enforces per-queue message limits by deleting
/// the oldest messages when a queue exceeds the configured maximum. Runs every 5 minutes.
/// </summary>
internal sealed class QueueOverflowCleanupTask(
    PostgresConnectionManager connectionManager,
    IReadOnlyPostgresSchemaOptions schemaOptions,
    ILogger logger,
    int messageLimit = 100_000)
    : PostgresBackgroundTask(connectionManager, schemaOptions, logger)
{
    /// <inheritdoc />
    protected override TimeSpan Interval => TimeSpan.FromMinutes(5);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await using var connection = await ConnectionManager.OpenConnectionAsync(ct);
        await using var command = connection.CreateCommand();

        command.CommandText =
            $"""
            WITH ranked AS (
                SELECT transport_message_id,
                       ROW_NUMBER() OVER (PARTITION BY queue_id ORDER BY sent_time DESC) AS rn
                FROM {SchemaOptions.MessageTable}
            )
            DELETE FROM {SchemaOptions.MessageTable}
            WHERE transport_message_id IN (
                SELECT transport_message_id FROM ranked WHERE rn > @limit
            );
            """;

        command.Parameters.Add(new("limit", NpgsqlDbType.Integer) { Value = messageLimit });

        var deleted = await command.ExecuteNonQueryAsync(ct);

        if (deleted > 0)
        {
            Logger.QueueOverflowCleanup(deleted, messageLimit);
        }
    }
}

internal static partial class Logs
{
    [LoggerMessage(LogLevel.Information, "Queue overflow cleanup removed {DeletedCount} messages exceeding limit of {MessageLimit}.")]
    public static partial void QueueOverflowCleanup(this ILogger logger, int deletedCount, int messageLimit);
}
