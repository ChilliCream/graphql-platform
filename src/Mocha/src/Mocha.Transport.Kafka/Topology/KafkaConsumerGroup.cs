namespace Mocha.Transport.Kafka;

/// <summary>
/// Represents a Kafka consumer group, modeling the group identity as a topology resource
/// for endpoint source and address resolution.
/// </summary>
public sealed class KafkaConsumerGroup : TopologyResource
{
    /// <summary>
    /// Gets the consumer group identifier.
    /// </summary>
    public string GroupId { get; private set; } = null!;

    /// <summary>
    /// Gets the topic that this consumer group subscribes to.
    /// </summary>
    public KafkaTopic Topic { get; private set; } = null!;

    /// <summary>
    /// Initializes the consumer group with the specified group ID, topic, and topology.
    /// </summary>
    /// <param name="groupId">The consumer group identifier.</param>
    /// <param name="topic">The topic this consumer group subscribes to.</param>
    /// <param name="topology">The topology that owns this resource.</param>
    public void Initialize(string groupId, KafkaTopic topic, KafkaMessagingTopology topology)
    {
        GroupId = groupId;
        Topic = topic;
        Topology = topology;
        Address = new Uri(topology.Address, $"cg/{GroupId}");
    }

    /// <inheritdoc />
    protected override void OnInitialize(TopologyConfiguration configuration)
    {
        // Initialization is handled by the typed Initialize overload.
    }
}
