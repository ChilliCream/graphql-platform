using Microsoft.Extensions.Logging;

namespace Mocha.Transport.Postgres.Tasks;

/// <summary>
/// A background task that periodically queries and logs topic statistics including
/// the number of subscriptions per topic. Runs every 5 minutes.
/// </summary>
internal sealed class TopicMonitoringTask(
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
        var naming = SchemaOptions;
        await using var connection = await ConnectionManager.OpenConnectionAsync(ct);
        await using var command = connection.CreateCommand();

        command.CommandText =
            $"""
            SELECT t.id, t.name, COUNT(s.id) AS subscription_count
            FROM {naming.TopicTable} t
            LEFT JOIN {naming.QueueSubscriptionTable} s ON s.source_id = t.id
            GROUP BY t.id, t.name;
            """;

        await using var reader = await command.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            var topicId = reader.GetInt64(0);
            var topicName = reader.GetString(1);
            var subscriptionCount = reader.GetInt64(2);

            Logger.TopicStatistics(topicName, topicId, subscriptionCount);
        }
    }
}

internal static partial class Logs
{
    [LoggerMessage(LogLevel.Information, "Topic {TopicName} (id={TopicId}): {SubscriptionCount} subscriptions.")]
    public static partial void TopicStatistics(this ILogger logger, string topicName, long topicId, long subscriptionCount);
}
