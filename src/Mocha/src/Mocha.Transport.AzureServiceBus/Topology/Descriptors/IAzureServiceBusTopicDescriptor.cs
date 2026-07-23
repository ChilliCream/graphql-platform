namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Fluent interface for configuring an Azure Service Bus topic.
/// </summary>
public interface IAzureServiceBusTopicDescriptor : IMessagingDescriptor<AzureServiceBusTopicConfiguration>
{
    /// <summary>
    /// Sets the name of the topic.
    /// </summary>
    /// <param name="name">The topic name.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IAzureServiceBusTopicDescriptor Name(string name);

    /// <summary>
    /// Sets whether the topic should be automatically provisioned.
    /// </summary>
    /// <param name="autoProvision">True to enable auto-provisioning (default: true).</param>
    /// <returns>The descriptor for method chaining.</returns>
    IAzureServiceBusTopicDescriptor AutoProvision(bool autoProvision = true);

    /// <summary>
    /// Sets the default time-to-live applied to messages that do not specify their own.
    /// </summary>
    IAzureServiceBusTopicDescriptor WithDefaultMessageTimeToLive(TimeSpan defaultMessageTimeToLive);

    /// <summary>
    /// Sets the maximum size of the topic in megabytes. Use the SDK's
    /// <c>MaxSizeInMegabytes</c> contract — there is no gigabyte-based property.
    /// </summary>
    IAzureServiceBusTopicDescriptor WithMaxSizeInMegabytes(long maxSizeInMegabytes);

    /// <summary>
    /// Sets whether the topic is partitioned. Must be set at creation time and cannot be altered later.
    /// </summary>
    IAzureServiceBusTopicDescriptor WithEnablePartitioning(bool enablePartitioning = true);

    /// <summary>
    /// Sets whether the topic enforces duplicate detection across the configured history window.
    /// </summary>
    IAzureServiceBusTopicDescriptor WithRequiresDuplicateDetection(bool requiresDuplicateDetection = true);

    /// <summary>
    /// Sets the time window over which duplicate detection is performed. Has no effect unless
    /// duplicate detection is enabled.
    /// </summary>
    IAzureServiceBusTopicDescriptor WithDuplicateDetectionHistoryTimeWindow(TimeSpan duplicateDetectionHistoryTimeWindow);

    /// <summary>
    /// Controls how long the topic waits without activity before being automatically deleted.
    /// </summary>
    IAzureServiceBusTopicDescriptor WithAutoDeleteOnIdle(TimeSpan autoDeleteOnIdle);

    /// <summary>
    /// Sets whether the topic preserves ordering across partitioned subscriptions.
    /// </summary>
    IAzureServiceBusTopicDescriptor WithSupportOrdering(bool supportOrdering = true);
}
