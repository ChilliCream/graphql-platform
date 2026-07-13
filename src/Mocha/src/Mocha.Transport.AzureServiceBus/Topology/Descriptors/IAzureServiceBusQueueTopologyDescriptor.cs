namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Fluent interface for configuring an Azure Service Bus queue.
/// </summary>
public interface IAzureServiceBusQueueTopologyDescriptor : IMessagingDescriptor<AzureServiceBusQueueConfiguration>
{
    /// <summary>
    /// Sets the name of the queue.
    /// </summary>
    /// <param name="name">The queue name.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IAzureServiceBusQueueTopologyDescriptor Name(string name);

    /// <summary>
    /// Compatibility toggle for <see cref="WithAutoDeleteOnIdle"/>. Azure Service Bus has no
    /// separate auto-delete flag: <c>true</c> alone configures no broker option, while <c>false</c>
    /// suppresses a configured idle deletion policy.
    /// </summary>
    /// <param name="autoDelete">Whether a configured idle deletion policy is enabled.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IAzureServiceBusQueueTopologyDescriptor AutoDelete(bool autoDelete = true);

    /// <summary>
    /// Sets whether the queue should be automatically provisioned.
    /// </summary>
    /// <param name="autoProvision">True to enable auto-provisioning (default: true).</param>
    /// <returns>The descriptor for method chaining.</returns>
    IAzureServiceBusQueueTopologyDescriptor AutoProvision(bool autoProvision = true);

    /// <summary>
    /// Controls how long the queue waits without activity before being automatically deleted.
    /// This is the broker option that enables deletion. <see cref="AutoDelete"/> can suppress it.
    /// </summary>
    IAzureServiceBusQueueTopologyDescriptor WithAutoDeleteOnIdle(TimeSpan autoDeleteOnIdle);

    /// <summary>
    /// Sets the duration that a message lock is held by a receiver before the broker
    /// makes the message available for redelivery.
    /// </summary>
    IAzureServiceBusQueueTopologyDescriptor WithLockDuration(TimeSpan lockDuration);

    /// <summary>
    /// Sets the maximum number of times a message will be delivered before it is moved to the dead-letter queue.
    /// </summary>
    IAzureServiceBusQueueTopologyDescriptor WithMaxDeliveryCount(int maxDeliveryCount);

    /// <summary>
    /// Sets the default time-to-live applied to messages that do not specify their own.
    /// </summary>
    IAzureServiceBusQueueTopologyDescriptor WithDefaultMessageTimeToLive(TimeSpan defaultMessageTimeToLive);

    /// <summary>
    /// Sets the maximum size of the queue in megabytes. Use the SDK's
    /// <c>MaxSizeInMegabytes</c> contract. There is no gigabyte-based property.
    /// </summary>
    IAzureServiceBusQueueTopologyDescriptor WithMaxSizeInMegabytes(long maxSizeInMegabytes);

    /// <summary>
    /// Sets whether the queue requires sessions. Once provisioned, this cannot be changed.
    /// </summary>
    IAzureServiceBusQueueTopologyDescriptor WithRequiresSession(bool requiresSession = true);

    /// <summary>
    /// Sets whether the queue is partitioned. Must be set at creation time and cannot be altered later.
    /// </summary>
    IAzureServiceBusQueueTopologyDescriptor WithEnablePartitioning(bool enablePartitioning = true);

    /// <summary>
    /// Sets the entity to which messages received on this queue are auto-forwarded.
    /// </summary>
    /// <param name="entityName">The destination entity name (queue or topic).</param>
    IAzureServiceBusQueueTopologyDescriptor WithForwardTo(string entityName);

    /// <summary>
    /// Sets the entity to which dead-lettered messages from this queue are auto-forwarded.
    /// </summary>
    /// <param name="entityName">The destination entity name (queue or topic).</param>
    IAzureServiceBusQueueTopologyDescriptor WithForwardDeadLetteredMessagesTo(string entityName);

    /// <summary>
    /// Sets whether expired messages should be moved to the dead-letter queue instead of being dropped.
    /// </summary>
    IAzureServiceBusQueueTopologyDescriptor WithDeadLetteringOnMessageExpiration(
        bool deadLetteringOnMessageExpiration = true);
}
