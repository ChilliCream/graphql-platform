using Microsoft.Extensions.Logging;
using Mocha.Middlewares;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Writes the log entries about the consumers of one RabbitMQ connection.
/// </summary>
/// <param name="logger">The logger that receives the entries.</param>
/// <param name="timeProvider">The clock used for the elapsed times in the entries.</param>
internal sealed partial class RabbitMQConsumerLogger(ILogger logger, TimeProvider timeProvider)
{
    public void ConsumerAttached(RabbitMQConsumer consumer, RabbitMQConsumerAttachment attachment, long detachedSince)
        => ConsumerAttached(
            attachment.Tag,
            consumer.QueueName,
            timeProvider.GetElapsedTime(detachedSince),
            consumer.MaxPrefetch,
            consumer.MaxConcurrency);

    public void ConsumerAttachFailed(
        RabbitMQConsumer consumer,
        Exception exception,
        int attempt,
        long detachedSince,
        TimeSpan delay)
        => ConsumerAttachFailed(
            exception,
            exception.IsPermanentFailure() ? LogLevel.Error : LogLevel.Warning,
            consumer.QueueName,
            attempt,
            timeProvider.GetElapsedTime(detachedSince),
            delay);

    /// <summary>
    /// Writes that a consumer lost its channel, or that the broker cancelled it when
    /// <paramref name="reason"/> is <c>null</c>.
    /// </summary>
    public void ConsumerLost(
        RabbitMQConsumer consumer,
        ShutdownEventArgs? reason,
        bool isConnectionLost,
        TimeSpan delay)
    {
        var cancelledHandlers = consumer.RunningHandlers;
        if (reason is null)
        {
            ConsumerCancelled(consumer.QueueName, cancelledHandlers, delay);
            return;
        }

        // The loss of the connection has its own entry. This one adds what happened to running handlers.
        ConsumerChannelLost(
            isConnectionLost && cancelledHandlers == 0 ? LogLevel.Debug : LogLevel.Warning,
            consumer.QueueName,
            reason.ReplyCode,
            reason.ReplyText,
            reason.Initiator,
            cancelledHandlers,
            delay);
    }

    public void ConsumerBlockedByHandlers(RabbitMQConsumer consumer, TimeSpan gracePeriod)
        => ConsumerBlockedByHandlers(consumer.QueueName, consumer.MaxConcurrency, gracePeriod);

    public void ConsumerStopped(RabbitMQConsumer consumer, long stopStarted, int runningHandlers)
        => ConsumerStopped(
            runningHandlers > 0 ? LogLevel.Warning : LogLevel.Information,
            consumer.QueueName,
            timeProvider.GetElapsedTime(stopStarted),
            runningHandlers);

    public void ConsumerFailed(RabbitMQConsumer consumer, Exception exception)
        => ConsumerFailed(exception, consumer.QueueName);

    public void DeliveryFailed(Exception exception, string queue, BasicDeliverEventArgs delivery)
        => DeliveryFailed(
            exception,
            delivery.BasicProperties.MessageId,
            queue,
            delivery.ConsumerTag,
            delivery.DeliveryTag,
            delivery.BasicProperties.CorrelationId);

    /// <summary>
    /// Writes that a message was not settled because its channel is closed. <paramref name="settlement"/> is
    /// <c>ack</c> or <c>nack</c>.
    /// </summary>
    public void SettlementSkipped(
        IReceiveContext context,
        string? queue,
        string settlement,
        BasicDeliverEventArgs delivery)
    {
        using (logger.BeginMessageScope(LogLevel.Information, context))
        {
            SettlementSkipped(
                queue,
                settlement,
                delivery.BasicProperties.MessageId ?? context.MessageId,
                delivery.ConsumerTag,
                delivery.DeliveryTag,
                delivery.Redelivered);
        }
    }

    [LoggerMessage(
        LogLevel.Information,
        "RabbitMQ consumer of {Queue} attached after {Elapsed} (tag {ConsumerTag}, prefetch "
            + "{MaxPrefetch}, concurrency {MaxConcurrency})")]
    private partial void ConsumerAttached(
        string consumerTag,
        string queue,
        TimeSpan elapsed,
        ushort maxPrefetch,
        ushort maxConcurrency);

    [LoggerMessage(LogLevel.Error, "RabbitMQ consumer of {Queue} could not attach; its endpoint does not start")]
    public partial void ConsumerRegistrationFailed(Exception exception, string queue);

    [LoggerMessage(
        LogLevel.Error,
        "RabbitMQ consumer of {Queue} blocked: all {MaxConcurrency} handlers of the lost channel still "
            + "run after {GracePeriod}; waiting for one to return")]
    private partial void ConsumerBlockedByHandlers(string queue, ushort maxConcurrency, TimeSpan gracePeriod);

    [LoggerMessage(
        Message = "RabbitMQ consumer of {Queue} stopped after {Elapsed}; {RunningHandlers} handlers did not return in time")]
    private partial void ConsumerStopped(LogLevel level, string queue, TimeSpan elapsed, int runningHandlers);

    [LoggerMessage(LogLevel.Critical, "RabbitMQ consumer of {Queue} failed unexpectedly; it no longer consumes")]
    private partial void ConsumerFailed(Exception exception, string queue);

    [LoggerMessage(LogLevel.Error, "RabbitMQ consumer of {Queue}: stopping failed")]
    public partial void ConsumerStopFailed(Exception exception, string queue);

    [LoggerMessage(LogLevel.Error, "RabbitMQ consumer of {Queue}: a cancellation callback of a handler threw")]
    public partial void HandlerCancellationFailed(Exception exception, string queue);

    [LoggerMessage(LogLevel.Warning, "RabbitMQ queue {Queue}: deleting failed")]
    public partial void QueueDeleteFailed(Exception exception, string queue);

    [LoggerMessage(
        Message = "RabbitMQ consumer of {Queue} detached for {Elapsed}, attempt {Attempt} failed; retrying in "
            + "{Delay}")]
    private partial void ConsumerAttachFailed(
        Exception exception,
        LogLevel level,
        string queue,
        int attempt,
        TimeSpan elapsed,
        TimeSpan delay);

    [LoggerMessage(
        Message = "RabbitMQ consumer of {Queue} lost its channel: {ReplyCode} {ReplyText} (initiator {Initiator}); "
            + "{CancelledHandlers} handlers cancelled, their messages can be delivered again, attaching again in "
            + "{Delay}")]
    private partial void ConsumerChannelLost(
        LogLevel level,
        string queue,
        ushort replyCode,
        string replyText,
        ShutdownInitiator initiator,
        int cancelledHandlers,
        TimeSpan delay);

    [LoggerMessage(
        LogLevel.Warning,
        "RabbitMQ consumer of {Queue} cancelled by the broker; {CancelledHandlers} handlers cancelled, "
            + "attaching again in {Delay}")]
    private partial void ConsumerCancelled(string queue, int cancelledHandlers, TimeSpan delay);

    [LoggerMessage(
        LogLevel.Error,
        "RabbitMQ consumer of {Queue}: handling message {MessageId} failed (tag {ConsumerTag}, delivery "
            + "{DeliveryTag}, correlation {CorrelationId})")]
    private partial void DeliveryFailed(
        Exception exception,
        string? messageId,
        string queue,
        string consumerTag,
        ulong deliveryTag,
        string? correlationId);

    [LoggerMessage(
        LogLevel.Information,
        "RabbitMQ consumer of {Queue}: {Settlement} of message {MessageId} skipped, channel closed (tag "
            + "{ConsumerTag}, delivery {DeliveryTag}, redelivered {Redelivered}); the message stays "
            + "unacknowledged and can be delivered again")]
    private partial void SettlementSkipped(
        string? queue,
        string settlement,
        string? messageId,
        string consumerTag,
        ulong deliveryTag,
        bool redelivered);
}
