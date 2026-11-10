using Microsoft.Extensions.Logging;

namespace Mocha;

internal static partial class MessagingLogs
{
    [LoggerMessage(LogLevel.Information, "Retrying message {MessageId} in consumer {Consumer} (attempt {Attempt}) after {Delay}")]
    public static partial void RetryingMessage(this ILogger logger, Exception exception, string? messageId, string consumer, int attempt, TimeSpan delay);

    [LoggerMessage(LogLevel.Warning, "Preparing redelivery of message {MessageId} (attempt {Attempt}) with requested delay {Delay}")]
    public static partial void RedeliveringMessage(this ILogger logger, Exception exception, string? messageId, int attempt, TimeSpan delay);

    [LoggerMessage(LogLevel.Information, "Dispatched redelivery of message {MessageId} (attempt {Attempt}) with scheduled time {ScheduledTime}")]
    public static partial void MessageRedelivered(this ILogger logger, string? messageId, int attempt, DateTimeOffset? scheduledTime);

    [LoggerMessage(LogLevel.Warning, "Discarded message {MessageId} according to its exception policy")]
    public static partial void MessageDiscarded(this ILogger logger, Exception exception, string? messageId);

    [LoggerMessage(LogLevel.Error, "Message {MessageId} failed with fault {FaultId}; fault destination is {DestinationAddress}")]
    public static partial void MessageFaulted(this ILogger logger, Exception exception, string? messageId, Guid faultId, string? destinationAddress);

    [LoggerMessage(LogLevel.Information, "Delivered fault {FaultId} for message {MessageId} to {DestinationAddress}")]
    public static partial void FaultDelivered(this ILogger logger, string? messageId, Guid faultId, Uri destinationAddress);

    [LoggerMessage(LogLevel.Error, "Message {MessageId} failed and fault {FaultId} could not be sent to {DestinationAddress}")]
    public static partial void FaultDeliveryFailed(this ILogger logger, Exception exception, string? messageId, Guid faultId, string? destinationAddress);

    [LoggerMessage(LogLevel.Warning, "Moved unhandled message {MessageId} to {DestinationAddress}")]
    public static partial void MessageSkipped(this ILogger logger, string? messageId, Uri destinationAddress);

    [LoggerMessage(LogLevel.Warning, "Circuit for endpoint {Endpoint} is open; recovery will be tested after {Delay}")]
    public static partial void CircuitOpen(this ILogger logger, Exception? exception, string endpoint, TimeSpan delay);

    [LoggerMessage(LogLevel.Information, "Circuit for endpoint {Endpoint} closed; processing resumed")]
    public static partial void CircuitClosed(this ILogger logger, string endpoint);

    [LoggerMessage(LogLevel.Information, "Circuit for endpoint {Endpoint} is testing recovery")]
    public static partial void CircuitHalfOpen(this ILogger logger, string endpoint);
}
