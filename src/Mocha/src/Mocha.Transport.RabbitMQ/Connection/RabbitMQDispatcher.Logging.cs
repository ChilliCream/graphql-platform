using Microsoft.Extensions.Logging;

namespace Mocha.Transport.RabbitMQ;

internal static partial class Logs
{
    [LoggerMessage(LogLevel.Warning, "Publishing message {MessageId} is still waiting for {Phase} after {Elapsed}")]
    public static partial void PublishStalled(
        this ILogger logger,
        string? messageId,
        PublishPhase phase,
        TimeSpan elapsed);

    [LoggerMessage(LogLevel.Debug, "Publishing message {MessageId}: waiting for {Phase}")]
    public static partial void PublishWaiting(this ILogger logger, string? messageId, PublishPhase phase);

    [LoggerMessage(Message = "Publish of message {MessageId} finished with {Outcome} in phase {Phase} after {Elapsed} and {Retries} retries")]
    public static partial void PublishFinished(
        this ILogger logger,
        Exception? exception,
        LogLevel level,
        string? messageId,
        PublishOutcome outcome,
        PublishPhase phase,
        int retries,
        TimeSpan elapsed);

    [LoggerMessage(LogLevel.Debug, "Created publisher channel {ChannelNumber} on {ConnectionName}")]
    public static partial void CreatedNewChannel(this ILogger logger, string connectionName, int channelNumber);

    [LoggerMessage(LogLevel.Debug, "Publisher channel capacity reached on {ConnectionName}")]
    public static partial void PublisherChannelLimitReached(this ILogger logger, Exception ex, string connectionName);

    [LoggerMessage(LogLevel.Warning, "Failed to open publisher channel on {ConnectionName}. Closing the connection and starting recovery")]
    public static partial void PublisherChannelCreationFailed(this ILogger logger, Exception ex, string connectionName);

    [LoggerMessage(
        LogLevel.Debug,
        "Publish to endpoint {Endpoint} on {ConnectionName} failed during connection recovery (attempt {Attempt}). Retrying in {Delay}")]
    public static partial void PublishFailedRetrying(
        this ILogger logger,
        Exception ex,
        string connectionName,
        string endpoint,
        int attempt,
        TimeSpan delay);

    [LoggerMessage(
        LogLevel.Warning,
        "The broker returned a message for queue {Queue} on {ConnectionName}. Repairing its configured topology")]
    public static partial void ReturnedQueueRepairStarted(this ILogger logger, string connectionName, string queue);

    [LoggerMessage(
        LogLevel.Information,
        "Restored configured topology for queue {Queue} on {ConnectionName}. Retrying the returned message")]
    public static partial void RedeclaredQueueOfReturnedMessage(this ILogger logger, string connectionName, string queue);
}
