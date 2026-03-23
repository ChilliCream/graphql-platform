using Microsoft.Extensions.Logging;

namespace Mocha.Transport.Postgres.Tasks;

/// <summary>
/// A background task that periodically queries and logs queue statistics including
/// message counts, scheduled message counts, and message age. Runs every 5 minutes.
/// </summary>
internal sealed class QueueMonitoringTask(
    PostgresConnectionManager connectionManager,
    IReadOnlyPostgresSchemaOptions schemaOptions,
    ILogger logger)
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
             SELECT q.id, q.name, q.consumer_id,
                    COUNT(m.transport_message_id) AS message_count,
                    COUNT(CASE WHEN m.scheduled_time IS NOT NULL AND m.scheduled_time > now() THEN 1 END) AS scheduled_count,
                    MIN(m.sent_time) AS oldest_message,
                    MAX(m.sent_time) AS newest_message
             FROM {SchemaOptions.QueueTable} q
             LEFT JOIN {SchemaOptions.MessageTable} m ON m.queue_id = q.id
             GROUP BY q.id, q.name, q.consumer_id;
             """;

        await using var reader = await command.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            var queueId = reader.GetInt64(0);
            var queueName = reader.GetString(1);
            var isTemporary = !reader.IsDBNull(2);
            var messageCount = reader.GetInt64(3);
            var scheduledCount = reader.GetInt64(4);
            var oldestMessage = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5);
            var newestMessage = reader.IsDBNull(6) ? (DateTime?)null : reader.GetDateTime(6);

            Logger.QueueStatistics(
                queueName,
                queueId,
                isTemporary,
                messageCount,
                scheduledCount,
                oldestMessage,
                newestMessage);
        }
    }
}

internal static partial class Logs
{
    [LoggerMessage(LogLevel.Information,
        "Queue {QueueName} (id={QueueId}, temporary={IsTemporary}): {MessageCount} messages, {ScheduledCount} scheduled, oldest={OldestMessage}, newest={NewestMessage}.")]
    public static partial void QueueStatistics(
        this ILogger logger,
        string queueName,
        long queueId,
        bool isTemporary,
        long messageCount,
        long scheduledCount,
        DateTime? oldestMessage,
        DateTime? newestMessage);
}
