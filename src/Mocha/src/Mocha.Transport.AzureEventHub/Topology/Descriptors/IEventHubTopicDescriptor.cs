namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Fluent interface for configuring an Event Hub topic (entity).
/// </summary>
public interface IEventHubTopicDescriptor : IMessagingDescriptor<EventHubTopicConfiguration>
{
    /// <summary>
    /// Sets the number of partitions for the Event Hub.
    /// </summary>
    /// <param name="partitionCount">The number of partitions.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IEventHubTopicDescriptor PartitionCount(int partitionCount);

    /// <summary>
    /// Sets whether the topic should be automatically provisioned.
    /// </summary>
    /// <param name="autoProvision"><c>true</c> to enable auto-provisioning (default).</param>
    /// <returns>The descriptor for method chaining.</returns>
    IEventHubTopicDescriptor AutoProvision(bool autoProvision = true);
}
