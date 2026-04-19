using Azure.Messaging.ServiceBus;

namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Azure Service Bus specific message context exposed on the receive pipeline for handlers that
/// need native broker primitives — dead-lettering with reason codes, explicit abandon with
/// property modifications.
/// </summary>
/// <remarks>
/// Resolve this from the active <see cref="IMessageContext"/> via the
/// <see cref="AzureServiceBusContextExtensions.AzureServiceBus"/> extension. The instance is
/// pooled together with the receive context and is only valid for the duration of the handler
/// invocation.
/// </remarks>
public interface IAzureServiceBusMessageContext
{
    /// <summary>
    /// Gets the raw <see cref="ServiceBusReceivedMessage"/> as delivered by the broker.
    /// </summary>
    ServiceBusReceivedMessage Message { get; }

    /// <summary>
    /// Gets the entity path (queue or subscription) the message was received from.
    /// </summary>
    string EntityPath { get; }

    /// <summary>
    /// Gets the broker-tracked delivery count for this message.
    /// </summary>
    int DeliveryCount { get; }

    /// <summary>
    /// Gets the absolute time at which the broker-managed lock on this message expires.
    /// </summary>
    DateTimeOffset LockedUntil { get; }

    /// <summary>
    /// Moves the current message to the entity's dead-letter queue with a reason and optional
    /// description and custom properties.
    /// </summary>
    /// <param name="reason">A short, machine-readable reason code (stored on the message as <c>DeadLetterReason</c>).</param>
    /// <param name="description">An optional human-readable description (stored as <c>DeadLetterErrorDescription</c>).</param>
    /// <param name="properties">Optional custom application properties to attach to the dead-lettered copy.</param>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    Task DeadLetterAsync(
        string reason,
        string? description = null,
        IDictionary<string, object>? properties = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Abandons the current message, returning it to the queue for redelivery and optionally
    /// modifying its application properties.
    /// </summary>
    /// <param name="propertiesToModify">Optional application properties to update on the message.</param>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    Task AbandonAsync(
        IDictionary<string, object>? propertiesToModify = null,
        CancellationToken cancellationToken = default);
}
