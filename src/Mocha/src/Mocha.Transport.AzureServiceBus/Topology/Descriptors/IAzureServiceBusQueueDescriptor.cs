namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Fluent interface for configuring an Azure Service Bus queue.
/// </summary>
public interface IAzureServiceBusQueueDescriptor : IMessagingDescriptor<AzureServiceBusQueueConfiguration>
{
    /// <summary>
    /// Sets the name of the queue.
    /// </summary>
    /// <param name="name">The queue name.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IAzureServiceBusQueueDescriptor Name(string name);

    /// <summary>
    /// Sets whether the queue is automatically deleted when no longer in use.
    /// </summary>
    /// <param name="autoDelete">True to enable auto-deletion (default: true).</param>
    /// <returns>The descriptor for method chaining.</returns>
    IAzureServiceBusQueueDescriptor AutoDelete(bool autoDelete = true);

    /// <summary>
    /// Sets whether the queue should be automatically provisioned.
    /// </summary>
    /// <param name="autoProvision">True to enable auto-provisioning (default: true).</param>
    /// <returns>The descriptor for method chaining.</returns>
    IAzureServiceBusQueueDescriptor AutoProvision(bool autoProvision = true);

    /// <summary>
    /// Controls how long the queue waits without activity before being automatically deleted.
    /// Has no effect unless <see cref="AutoDelete"/> is enabled.
    /// </summary>
    IAzureServiceBusQueueDescriptor WithAutoDeleteOnIdle(TimeSpan autoDeleteOnIdle);

    /// <summary>
    /// Sets the duration that a message lock is held by a receiver before the broker
    /// makes the message available for redelivery.
    /// </summary>
    IAzureServiceBusQueueDescriptor WithLockDuration(TimeSpan lockDuration);

    /// <summary>
    /// Sets the maximum number of times a message will be delivered before it is moved to the dead-letter queue.
    /// </summary>
    IAzureServiceBusQueueDescriptor WithMaxDeliveryCount(int maxDeliveryCount);

    /// <summary>
    /// Sets the default time-to-live applied to messages that do not specify their own.
    /// </summary>
    IAzureServiceBusQueueDescriptor WithDefaultMessageTimeToLive(TimeSpan defaultMessageTimeToLive);

    /// <summary>
    /// Sets the maximum size of the queue in megabytes. Use the SDK's
    /// <c>MaxSizeInMegabytes</c> contract — there is no gigabyte-based property.
    /// </summary>
    IAzureServiceBusQueueDescriptor WithMaxSizeInMegabytes(long maxSizeInMegabytes);

    /// <summary>
    /// Sets whether the queue requires sessions. Once provisioned, this cannot be changed.
    /// </summary>
    IAzureServiceBusQueueDescriptor WithRequiresSession(bool requiresSession = true);

    /// <summary>
    /// Sets whether the queue is partitioned. Must be set at creation time and cannot be altered later.
    /// </summary>
    IAzureServiceBusQueueDescriptor WithEnablePartitioning(bool enablePartitioning = true);

    /// <summary>
    /// Sets the entity to which messages received on this queue are auto-forwarded.
    /// </summary>
    /// <param name="entityName">The destination entity name (queue or topic).</param>
    IAzureServiceBusQueueDescriptor WithForwardTo(string entityName);

    /// <summary>
    /// Sets the entity to which dead-lettered messages from this queue are auto-forwarded.
    /// </summary>
    /// <param name="entityName">The destination entity name (queue or topic).</param>
    IAzureServiceBusQueueDescriptor WithForwardDeadLetteredMessagesTo(string entityName);

    /// <summary>
    /// Sets whether expired messages should be moved to the dead-letter queue instead of being dropped.
    /// </summary>
    IAzureServiceBusQueueDescriptor WithDeadLetteringOnMessageExpiration(bool deadLetteringOnMessageExpiration = true);
}
