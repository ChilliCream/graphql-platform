using Microsoft.Extensions.Logging;

namespace Mocha.Transport.RabbitMQ;

internal static partial class Logs
{
    [LoggerMessage(
        LogLevel.Debug,
        "The broker confirmed message {MessageId} for endpoint {Endpoint} on {ConnectionName} after {Elapsed} "
            + "(attempt {Attempt})")]
    public static partial void PublishConfirmed(
        this ILogger logger,
        string? messageId,
        string endpoint,
        string connectionName,
        int attempt,
        TimeSpan elapsed);

    [LoggerMessage(
        LogLevel.Warning,
        "The broker confirmed message {MessageId} for endpoint {Endpoint} on {ConnectionName} only after "
            + "{Elapsed} (attempt {Attempt}), which is longer than {Threshold}")]
    public static partial void PublishConfirmedSlowly(
        this ILogger logger,
        string? messageId,
        string endpoint,
        string connectionName,
        int attempt,
        TimeSpan elapsed,
        TimeSpan threshold);

    [LoggerMessage(
        LogLevel.Error,
        "Publishing message {MessageId} to endpoint {Endpoint} on {ConnectionName} failed after {Elapsed} "
            + "(attempt {Attempt})")]
    public static partial void PublishFailed(
        this ILogger logger,
        Exception exception,
        string? messageId,
        string endpoint,
        string connectionName,
        int attempt,
        TimeSpan elapsed);

    [LoggerMessage(
        LogLevel.Debug,
        "Publishing message {MessageId} to endpoint {Endpoint} on {ConnectionName} was cancelled by the caller "
            + "after {Elapsed} (attempt {Attempt})")]
    public static partial void PublishCancelled(
        this ILogger logger,
        string? messageId,
        string endpoint,
        string connectionName,
        int attempt,
        TimeSpan elapsed);

    [LoggerMessage(
        LogLevel.Information,
        "Publishing message {MessageId} to endpoint {Endpoint} on {ConnectionName} failed (attempt {Attempt}) "
            + "and is attempted again in {Delay}. The broker delivers the message twice if it already has it. "
            + "Reason: {Reason}")]
    public static partial void PublishRetryScheduled(
        this ILogger logger,
        string? messageId,
        string endpoint,
        string connectionName,
        int attempt,
        TimeSpan delay,
        string reason);

    [LoggerMessage(
        LogLevel.Warning,
        "{Count} publishes on RabbitMQ connection {ConnectionName} have waited for the broker for at least "
            + "{Interval}. The connection is {ConnectionStatus}")]
    public static partial void PublishesStalled(
        this ILogger logger,
        string connectionName,
        long count,
        TimeSpan interval,
        string connectionStatus);

    [LoggerMessage(LogLevel.Debug, "Opened publisher channel {ChannelNumber} on {ConnectionName}")]
    public static partial void PublisherChannelOpened(this ILogger logger, string connectionName, int channelNumber);

    [LoggerMessage(
        LogLevel.Warning,
        "The broker returned message {MessageId} because queue {Queue} does not exist on {ConnectionName}. "
            + "Declaring the queue and its bindings again")]
    public static partial void MessageReturned(
        this ILogger logger,
        string? messageId,
        string queue,
        string connectionName);

    [LoggerMessage(
        LogLevel.Information,
        "Declared queue {Queue} and its bindings again on {ConnectionName}. Publishing message {MessageId} again")]
    public static partial void QueueRedeclared(
        this ILogger logger,
        string queue,
        string connectionName,
        string? messageId);
}
