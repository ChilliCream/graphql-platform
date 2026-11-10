using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ;

internal static partial class Logs
{
    [LoggerMessage(LogLevel.Information, "Attached consumer {ConsumerTag} to queue {Queue} with prefetch {Prefetch} and concurrency {Concurrency}")]
    public static partial void ConsumerAttached(this ILogger logger, string queue, string consumerTag, ushort prefetch, ushort concurrency);

    [LoggerMessage(LogLevel.Error, "Failed to register consumer for queue {Queue} on {ConnectionName}")]
    public static partial void ConsumerRegistrationFailed(
        this ILogger logger,
        Exception ex,
        string connectionName,
        string queue);

    [LoggerMessage(LogLevel.Error, "Error processing message {MessageId} from queue {Queue} on {ConnectionName}, consumer {ConsumerTag}, delivery {DeliveryTag}, correlation {CorrelationId}")]
    public static partial void ErrorProcessingMessageFromQueue(
        this ILogger logger,
        Exception ex,
        string connectionName,
        string queue,
        string? messageId,
        string? correlationId,
        string consumerTag,
        ulong deliveryTag);

    [LoggerMessage(LogLevel.Information, "Registered consumer for queue {Queue} on {ConnectionName}. Attached: {Attached}")]
    public static partial void RegisteredConsumerForQueue(this ILogger logger, string connectionName, string queue, bool attached);

    [LoggerMessage(LogLevel.Information, "Reconnecting {Count} consumers on {ConnectionName}")]
    public static partial void ReconnectConsumersAsync(this ILogger logger, string connectionName, int count);

    [LoggerMessage(LogLevel.Information, "Reconnected consumer for queue {Queue} on {ConnectionName}")]
    public static partial void ReconnectConsumerForQueue(this ILogger logger, string connectionName, string queue);

    [LoggerMessage(
        LogLevel.Warning,
        "Consumer for queue {Queue} on {ConnectionName} lost its channel. Initiator: {Initiator}, Reply: {ReplyCode} {ReplyText}")]
    public static partial void ConsumerChannelLost(
        this ILogger logger,
        string connectionName,
        string queue,
        ShutdownInitiator initiator,
        ushort replyCode,
        string replyText);

    [LoggerMessage(LogLevel.Warning, "Consumer for queue {Queue} on {ConnectionName} was cancelled by the broker")]
    public static partial void ConsumerCancelledByBroker(this ILogger logger, string connectionName, string queue);

    [LoggerMessage(LogLevel.Information, "Re-attached consumer for queue {Queue} on {ConnectionName} after {Elapsed}")]
    public static partial void ReattachedConsumerForQueue(this ILogger logger, string connectionName, string queue, TimeSpan elapsed);

    [LoggerMessage(Message = "Consumer for queue {Queue} on {ConnectionName} is still detached after {Elapsed} (attempt {Attempt}). Retrying in {Delay}")]
    public static partial void FailedToReattachConsumerForQueue(
        this ILogger logger,
        Exception ex,
        LogLevel level,
        string connectionName,
        string queue,
        int attempt,
        TimeSpan delay,
        TimeSpan elapsed);

    [LoggerMessage(Message = "Failed to reconnect consumer for queue {Queue} on {ConnectionName}. Consumer recovery will retry")]
    public static partial void FailedToReconnectConsumerForQueue(
        this ILogger logger,
        Exception ex,
        LogLevel level,
        string connectionName,
        string queue);

    [LoggerMessage(LogLevel.Debug, "Connection {ConnectionName} health check failed; ensuring recovery is running")]
    public static partial void ConnectionHealthCheckFailedAttemptingReconnection(this ILogger logger, string connectionName);

    [LoggerMessage(LogLevel.Trace, "Connection {ConnectionName} health check: OK")]
    public static partial void ConnectionHealthCheckOk(this ILogger logger, string connectionName);

    [LoggerMessage(LogLevel.Error, "Error during connection {ConnectionName} health check")]
    public static partial void ErrorDuringConnectionHealthCheck(this ILogger logger, Exception ex, string connectionName);

    [LoggerMessage(LogLevel.Error, "Error disposing consumer for queue {Queue} on {ConnectionName}")]
    public static partial void ErrorDisposingConsumer(this ILogger logger, Exception ex, string connectionName, string queue);

    [LoggerMessage(LogLevel.Warning, "Failed to delete queue {Queue} on {ConnectionName}")]
    public static partial void FailedToDeleteQueue(this ILogger logger, Exception ex, string connectionName, string queue);

    [LoggerMessage(LogLevel.Error, "Consumer for queue {Queue} on {ConnectionName} is unhealthy: all {Concurrency} handler slots are occupied by cancelled handlers. Processing resumes when a handler exits")]
    public static partial void ConsumerRecoveryBlocked(this ILogger logger, string connectionName, string queue, int concurrency);

    [LoggerMessage(LogLevel.Information, "Consumer for queue {Queue} on {ConnectionName} has handler capacity available again")]
    public static partial void ConsumerRecoveryResumed(this ILogger logger, string connectionName, string queue);

    [LoggerMessage(LogLevel.Error, "Error cancelling handlers for queue {Queue} on {ConnectionName}")]
    public static partial void ErrorCancellingConsumerHandlers(this ILogger logger, Exception exception, string connectionName, string queue);

    [LoggerMessage(LogLevel.Information, "Stopped consumer for queue {Queue} on {ConnectionName} after {Elapsed}; {ActiveHandlers} handlers remain active")]
    public static partial void ConsumerStopped(this ILogger logger, string connectionName, string queue, int activeHandlers, TimeSpan elapsed);

    [LoggerMessage(LogLevel.Information, "Draining consumer for queue {Queue} with {ActiveHandlers} active handlers")]
    public static partial void ConsumerDrainStarted(this ILogger logger, string queue, int activeHandlers);

    [LoggerMessage(LogLevel.Warning, "Consumer drain for queue {Queue} was cancelled after {Elapsed} with {ActiveHandlers} active handlers")]
    public static partial void ConsumerDrainCancelled(this ILogger logger, string queue, int activeHandlers, TimeSpan elapsed);
}
