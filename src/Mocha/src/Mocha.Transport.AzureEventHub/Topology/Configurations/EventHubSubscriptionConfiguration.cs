namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Configuration for an Event Hub subscription (consumer group).
/// </summary>
public sealed class EventHubSubscriptionConfiguration : TopologyConfiguration<EventHubMessagingTopology>
{
    /// <summary>
    /// Gets or sets the name of the Event Hub entity (topic) this subscription belongs to.
    /// </summary>
    public string TopicName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the consumer group name.
    /// Defaults to "$Default".
    /// </summary>
    public string ConsumerGroup { get; set; } = "$Default";

    /// <summary>
    /// Gets or sets a value indicating whether the consumer group should be automatically provisioned.
    /// When <c>true</c>, the consumer group will be created during topology provisioning.
    /// </summary>
    public bool? AutoProvision { get; set; }
}
