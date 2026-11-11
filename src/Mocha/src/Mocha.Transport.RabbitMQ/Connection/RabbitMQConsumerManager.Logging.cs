using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ;

internal static partial class Logs
{
    [LoggerMessage(
        LogLevel.Information,
        "RabbitMQ consumer of {Queue} attached after {Elapsed} (tag {ConsumerTag}, prefetch "
            + "{MaxPrefetch}, concurrency {MaxConcurrency})")]
    public static partial void ConsumerAttached(
        this ILogger logger,
        string consumerTag,
        string queue,
        TimeSpan elapsed,
        ushort maxPrefetch,
        ushort maxConcurrency);

    [LoggerMessage(
        Message = "RabbitMQ consumer of {Queue} detached for {Elapsed}, attempt {Attempt} failed; retrying in "
            + "{Delay}")]
    public static partial void ConsumerAttachFailed(
        this ILogger logger,
        Exception exception,
        LogLevel level,
        string queue,
        int attempt,
        TimeSpan elapsed,
        TimeSpan delay);

    [LoggerMessage(LogLevel.Error, "RabbitMQ consumer of {Queue} refused by the broker; its endpoint does not start")]
    public static partial void ConsumerRegistrationFailed(
        this ILogger logger,
        Exception exception,
        string queue);

    [LoggerMessage(
        Message = "RabbitMQ consumer of {Queue} lost its channel: {ReplyCode} {ReplyText} (initiator {Initiator}); "
            + "{CancelledHandlers} handlers cancelled, their messages are delivered again")]
    public static partial void ConsumerChannelLost(
        this ILogger logger,
        LogLevel level,
        string queue,
        ushort replyCode,
        string replyText,
        ShutdownInitiator initiator,
        int cancelledHandlers);

    [LoggerMessage(
        LogLevel.Warning,
        "RabbitMQ consumer of {Queue} cancelled by the broker; {CancelledHandlers} handlers cancelled, "
            + "attaching again")]
    public static partial void ConsumerCancelled(
        this ILogger logger,
        string queue,
        int cancelledHandlers);

    [LoggerMessage(
        LogLevel.Error,
        "RabbitMQ consumer of {Queue} blocked: all {MaxConcurrency} handlers of the lost channel still "
            + "run {GracePeriod} after their cancellation; waiting for one to return")]
    public static partial void ConsumerBlockedByHandlers(
        this ILogger logger,
        string queue,
        ushort maxConcurrency,
        TimeSpan gracePeriod);

    [LoggerMessage(LogLevel.Information, "RabbitMQ consumer of {Queue} unblocked after {Elapsed}; attaching again")]
    public static partial void ConsumerUnblocked(
        this ILogger logger,
        string queue,
        TimeSpan elapsed);

    [LoggerMessage(
        LogLevel.Information,
        "RabbitMQ consumer of {Queue} stopping; waiting for {RunningHandlers} handlers")]
    public static partial void ConsumerDrainStarted(
        this ILogger logger,
        string queue,
        int runningHandlers);

    [LoggerMessage(
        LogLevel.Warning,
        "RabbitMQ consumer of {Queue} stopped waiting after {Elapsed}; {RunningHandlers} handlers "
            + "cancelled, their messages return to the queue")]
    public static partial void ConsumerDrainTimedOut(
        this ILogger logger,
        string queue,
        int runningHandlers,
        TimeSpan elapsed);

    [LoggerMessage(
        LogLevel.Information,
        "RabbitMQ consumer of {Queue} stopped after {Elapsed} ({RunningHandlers} handlers still running)")]
    public static partial void ConsumerStopped(
        this ILogger logger,
        string queue,
        TimeSpan elapsed,
        int runningHandlers);

    [LoggerMessage(LogLevel.Critical, "RabbitMQ consumer of {Queue} failed unexpectedly; it no longer consumes")]
    public static partial void ConsumerFailed(
        this ILogger logger,
        Exception exception,
        string queue);

    [LoggerMessage(LogLevel.Error, "RabbitMQ consumer of {Queue}: stopping failed")]
    public static partial void ConsumerStopFailed(
        this ILogger logger,
        Exception exception,
        string queue);

    [LoggerMessage(
        LogLevel.Error,
        "RabbitMQ consumer of {Queue}: handling message {MessageId} failed (tag {ConsumerTag}, delivery "
            + "{DeliveryTag}, correlation {CorrelationId})")]
    public static partial void DeliveryFailed(
        this ILogger logger,
        Exception exception,
        string? messageId,
        string queue,
        string consumerTag,
        ulong deliveryTag,
        string? correlationId);

    [LoggerMessage(LogLevel.Error, "RabbitMQ consumer of {Queue}: cancelling its handlers failed")]
    public static partial void HandlerCancellationFailed(
        this ILogger logger,
        Exception exception,
        string queue);

    [LoggerMessage(LogLevel.Warning, "RabbitMQ queue {Queue}: deleting failed")]
    public static partial void QueueDeleteFailed(
        this ILogger logger,
        Exception exception,
        string queue);

    [LoggerMessage(
        LogLevel.Information,
        "RabbitMQ message {MessageId}: {Settlement} skipped, channel closed (tag {ConsumerTag}, delivery "
            + "{DeliveryTag}, exchange {Exchange}, routing key {RoutingKey}, redelivered {Redelivered}); the "
            + "broker delivers it again")]
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
