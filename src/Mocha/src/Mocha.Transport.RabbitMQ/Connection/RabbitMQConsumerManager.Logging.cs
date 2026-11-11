using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ;

internal static partial class Logs
{
    [LoggerMessage(
        LogLevel.Information,
        "Consumer {ConsumerTag} is attached to queue {Queue} on {ConnectionName} after {Elapsed} "
            + "(prefetch {MaxPrefetch}, concurrency {MaxConcurrency})")]
    public static partial void ConsumerAttached(
        this ILogger logger,
        string consumerTag,
        string queue,
        string connectionName,
        TimeSpan elapsed,
        ushort maxPrefetch,
        ushort maxConcurrency);

    [LoggerMessage(
        Message = "The consumer for queue {Queue} on {ConnectionName} is detached for {Elapsed}: attempt {Attempt} "
            + "failed. Retrying in {Delay}")]
    public static partial void ConsumerAttachFailed(
        this ILogger logger,
        Exception exception,
        LogLevel level,
        string queue,
        string connectionName,
        int attempt,
        TimeSpan elapsed,
        TimeSpan delay);

    [LoggerMessage(
        LogLevel.Error,
        "The broker refused the consumer for queue {Queue} on {ConnectionName}. Its endpoint does not start")]
    public static partial void ConsumerRegistrationFailed(
        this ILogger logger,
        Exception exception,
        string queue,
        string connectionName);

    [LoggerMessage(
        Message = "The consumer for queue {Queue} on {ConnectionName} lost its channel: {ReplyCode} {ReplyText} "
            + "(initiator: {Initiator}). {CancelledHandlers} running handlers were cancelled, and the broker "
            + "delivers their messages again")]
    public static partial void ConsumerChannelLost(
        this ILogger logger,
        LogLevel level,
        string queue,
        string connectionName,
        ushort replyCode,
        string replyText,
        ShutdownInitiator initiator,
        int cancelledHandlers);

    [LoggerMessage(
        LogLevel.Warning,
        "The broker cancelled the consumer for queue {Queue} on {ConnectionName}, which it does when the queue "
            + "is deleted. {CancelledHandlers} running handlers were cancelled. Attaching again")]
    public static partial void ConsumerCancelled(
        this ILogger logger,
        string queue,
        string connectionName,
        int cancelledHandlers);

    [LoggerMessage(
        LogLevel.Error,
        "The consumer for queue {Queue} on {ConnectionName} cannot attach: all {MaxConcurrency} handlers of a "
            + "lost channel are still running {GracePeriod} after they were cancelled. The endpoint consumes "
            + "again when one of them returns")]
    public static partial void ConsumerBlockedByHandlers(
        this ILogger logger,
        string queue,
        string connectionName,
        ushort maxConcurrency,
        TimeSpan gracePeriod);

    [LoggerMessage(
        LogLevel.Information,
        "A handler of the consumer for queue {Queue} on {ConnectionName} returned after {Elapsed}. "
            + "The consumer attaches again")]
    public static partial void ConsumerUnblocked(
        this ILogger logger,
        string queue,
        string connectionName,
        TimeSpan elapsed);

    [LoggerMessage(
        LogLevel.Information,
        "Stopping the consumer for queue {Queue} on {ConnectionName}: waiting for {RunningHandlers} running "
            + "handlers")]
    public static partial void ConsumerDrainStarted(
        this ILogger logger,
        string queue,
        string connectionName,
        int runningHandlers);

    [LoggerMessage(
        LogLevel.Warning,
        "The consumer for queue {Queue} on {ConnectionName} stopped waiting for its handlers after {Elapsed}. "
            + "{RunningHandlers} handlers were cancelled, and the broker returns their unacknowledged messages "
            + "to the queue")]
    public static partial void ConsumerDrainTimedOut(
        this ILogger logger,
        string queue,
        string connectionName,
        int runningHandlers,
        TimeSpan elapsed);

    [LoggerMessage(
        LogLevel.Information,
        "Stopped the consumer for queue {Queue} on {ConnectionName} after {Elapsed}. {RunningHandlers} "
            + "handlers are still running")]
    public static partial void ConsumerStopped(
        this ILogger logger,
        string queue,
        string connectionName,
        TimeSpan elapsed,
        int runningHandlers);

    [LoggerMessage(
        LogLevel.Critical,
        "The consumer for queue {Queue} on {ConnectionName} failed unexpectedly and no longer consumes")]
    public static partial void ConsumerFailed(
        this ILogger logger,
        Exception exception,
        string queue,
        string connectionName);

    [LoggerMessage(LogLevel.Error, "Stopping the consumer for queue {Queue} on {ConnectionName} failed")]
    public static partial void ConsumerStopFailed(
        this ILogger logger,
        Exception exception,
        string queue,
        string connectionName);

    [LoggerMessage(
        LogLevel.Error,
        "Handling message {MessageId} from queue {Queue} on {ConnectionName} failed (consumer {ConsumerTag}, "
            + "delivery {DeliveryTag}, correlation {CorrelationId})")]
    public static partial void DeliveryFailed(
        this ILogger logger,
        Exception exception,
        string? messageId,
        string queue,
        string connectionName,
        string consumerTag,
        ulong deliveryTag,
        string? correlationId);

    [LoggerMessage(
        LogLevel.Error,
        "Cancelling the handlers of the consumer for queue {Queue} on {ConnectionName} failed")]
    public static partial void HandlerCancellationFailed(
        this ILogger logger,
        Exception exception,
        string queue,
        string connectionName);

    [LoggerMessage(LogLevel.Warning, "Deleting queue {Queue} on {ConnectionName} failed")]
    public static partial void QueueDeleteFailed(
        this ILogger logger,
        Exception exception,
        string queue,
        string connectionName);

    [LoggerMessage(
        LogLevel.Information,
        "Skipped the {Settlement} of message {MessageId} because its channel closed (consumer {ConsumerTag}, "
            + "delivery {DeliveryTag}, exchange {Exchange}, routing key {RoutingKey}, redelivered: {Redelivered}). "
            + "The broker delivers the message again")]
    public static partial void SettlementSkipped(
        this ILogger logger,
        string settlement,
        string? messageId,
        string consumerTag,
        ulong deliveryTag,
        string exchange,
        string routingKey,
        bool redelivered);
}
