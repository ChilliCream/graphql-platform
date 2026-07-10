using Microsoft.Extensions.Logging;

namespace Mocha.Transport.Postgres.Tasks;

/// <summary>
/// A background task that periodically updates the consumer heartbeat timestamp
/// to indicate the consumer is still active. Runs every 10 seconds.
/// When the heartbeat detects that the consumer row was evicted, it invokes
/// an optional callback to trigger recovery.
/// </summary>
internal sealed class ConsumerHeartbeatTask(
    PostgresConsumerManager consumerManager,
    PostgresConnectionManager connectionManager,
    IReadOnlyPostgresSchemaOptions schemaOptions,
    ILogger logger,
    Func<CancellationToken, Task>? onEvicted = null)
    : PostgresBackgroundTask(connectionManager, schemaOptions, logger)
{
    /// <inheritdoc />
    protected override TimeSpan Interval => TimeSpan.FromSeconds(10);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var alive = await consumerManager.HeartbeatAsync(ct);

        if (!alive)
        {
            Logger.ConsumerEvicted(consumerManager.ConsumerId);

            if (onEvicted is not null)
            {
                await onEvicted(ct);
            }
        }
    }
}

internal static partial class Logs
{
    [LoggerMessage(LogLevel.Error, "Error sending consumer heartbeat.")]
    public static partial void ConsumerHeartbeatFailed(this ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Consumer {ConsumerId} was evicted by another pod. Initiating recovery.")]
    public static partial void ConsumerEvicted(this ILogger logger, Guid consumerId);
}
