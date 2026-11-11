using Microsoft.Extensions.Logging;

namespace Mocha.Transport.RabbitMQ;

internal static partial class Logs
{
    [LoggerMessage(
        LogLevel.Debug,
        "RabbitMQ publish of {MessageId} to {Endpoint} confirmed after {Elapsed} (attempt {Attempt})")]
    public static partial void PublishConfirmed(
        this ILogger logger,
        string? messageId,
        string endpoint,
        int attempt,
        TimeSpan elapsed);

    [LoggerMessage(
        LogLevel.Warning,
        "RabbitMQ publish of {MessageId} to {Endpoint} confirmed slowly after {Elapsed} (attempt "
            + "{Attempt}, threshold {Threshold})")]
    public static partial void PublishConfirmedSlowly(
        this ILogger logger,
        string? messageId,
        string endpoint,
        int attempt,
        TimeSpan elapsed,
        TimeSpan threshold);

    [LoggerMessage(
        LogLevel.Error,
        "RabbitMQ publish of {MessageId} to {Endpoint} failed after {Elapsed} (attempt {Attempt})")]
    public static partial void PublishFailed(
        this ILogger logger,
        Exception exception,
        string? messageId,
        string endpoint,
        int attempt,
        TimeSpan elapsed);

    [LoggerMessage(
        LogLevel.Debug,
        "RabbitMQ publish of {MessageId} to {Endpoint} cancelled by the caller after {Elapsed} (attempt "
            + "{Attempt})")]
    public static partial void PublishCancelled(
        this ILogger logger,
        string? messageId,
        string endpoint,
        int attempt,
        TimeSpan elapsed);

    [LoggerMessage(
        LogLevel.Information,
        "RabbitMQ publish of {MessageId} to {Endpoint} failed (attempt {Attempt}): {Reason}; retrying in "
            + "{Delay}, which can deliver it twice")]
    public static partial void PublishRetryScheduled(
        this ILogger logger,
        string? messageId,
        string endpoint,
        int attempt,
        TimeSpan delay,
        string reason);

    [LoggerMessage(
        LogLevel.Warning,
        "RabbitMQ {ConnectionRole} connection {ConnectionStatus}: {Count} publishes waiting for "
            + "{Interval} or longer")]
    public static partial void PublishesStalled(
        this ILogger logger,
        string connectionRole,
        long count,
        TimeSpan interval,
        string connectionStatus);

    [LoggerMessage(LogLevel.Debug, "RabbitMQ {ConnectionRole} connection opened publisher channel {ChannelNumber}")]
    public static partial void PublisherChannelOpened(this ILogger logger, string connectionRole, int channelNumber);

    [LoggerMessage(
        LogLevel.Warning,
        "RabbitMQ publish of {MessageId} returned: queue {Queue} does not exist; declaring it again")]
    public static partial void MessageReturned(
        this ILogger logger,
        string? messageId,
        string queue);

    [LoggerMessage(
        LogLevel.Information,
        "RabbitMQ queue {Queue} declared again with its bindings; publishing {MessageId} again")]
    public static partial void QueueRedeclared(
        this ILogger logger,
        string queue,
        string? messageId);
}
