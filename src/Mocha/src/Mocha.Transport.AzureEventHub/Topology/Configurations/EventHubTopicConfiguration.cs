namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Configuration for an Event Hub topic (entity).
/// </summary>
public sealed class EventHubTopicConfiguration : TopologyConfiguration<EventHubMessagingTopology>
{
    /// <summary>
    /// Gets or sets the name of the Event Hub entity.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of partitions for the Event Hub.
    /// When <c>null</c>, the Azure default is used.
    /// </summary>
    public int? PartitionCount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the topic should be automatically provisioned.
    /// When <c>true</c>, the Event Hub entity will be created during topology provisioning.
    /// </summary>
    public bool? AutoProvision { get; set; }
}
