namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Fluent interface for configuring an Azure Service Bus subscription (topic-to-queue forwarding).
/// </summary>
public interface IAzureServiceBusSubscriptionDescriptor : IMessagingDescriptor<AzureServiceBusSubscriptionConfiguration>
{
    /// <summary>
    /// Sets whether the subscription should be automatically provisioned.
    /// </summary>
    /// <param name="autoProvision">True to enable auto-provisioning (default: true).</param>
    /// <returns>The descriptor for method chaining.</returns>
    IAzureServiceBusSubscriptionDescriptor AutoProvision(bool autoProvision = true);

    /// <summary>
    /// Sets the duration that a message lock is held by a receiver before the broker
    /// makes the message available for redelivery.
    /// </summary>
    IAzureServiceBusSubscriptionDescriptor WithLockDuration(TimeSpan lockDuration);

    /// <summary>
    /// Sets the maximum number of times a message will be delivered before it is moved to the dead-letter queue.
    /// </summary>
    IAzureServiceBusSubscriptionDescriptor WithMaxDeliveryCount(int maxDeliveryCount);

    /// <summary>
    /// Sets the default time-to-live applied to messages that do not specify their own.
    /// </summary>
    IAzureServiceBusSubscriptionDescriptor WithDefaultMessageTimeToLive(TimeSpan defaultMessageTimeToLive);

    /// <summary>
    /// Sets whether the subscription requires sessions. Once provisioned, this cannot be changed.
    /// </summary>
    IAzureServiceBusSubscriptionDescriptor WithRequiresSession(bool requiresSession = true);

    /// <summary>
    /// Sets the entity to which messages received on this subscription are auto-forwarded.
    /// Overrides the default forward target derived from the destination queue.
    /// </summary>
    /// <param name="entityName">The destination entity name (queue or topic).</param>
    IAzureServiceBusSubscriptionDescriptor WithForwardTo(string entityName);

    /// <summary>
    /// Sets the entity to which dead-lettered messages from this subscription are auto-forwarded.
    /// </summary>
    /// <param name="entityName">The destination entity name (queue or topic).</param>
    IAzureServiceBusSubscriptionDescriptor WithForwardDeadLetteredMessagesTo(string entityName);

    /// <summary>
    /// Sets whether expired messages should be moved to the dead-letter queue instead of being dropped.
    /// </summary>
    IAzureServiceBusSubscriptionDescriptor WithDeadLetteringOnMessageExpiration(bool deadLetteringOnMessageExpiration = true);

    /// <summary>
    /// Controls how long the subscription waits without activity before being automatically deleted.
    /// </summary>
    IAzureServiceBusSubscriptionDescriptor WithAutoDeleteOnIdle(TimeSpan autoDeleteOnIdle);
}
