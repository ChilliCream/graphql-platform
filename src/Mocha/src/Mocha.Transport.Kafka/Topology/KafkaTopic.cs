namespace Mocha.Transport.Kafka;

/// <summary>
/// Represents a Kafka topic entity with its configuration.
/// </summary>
public sealed class KafkaTopic : TopologyResource
{
    /// <summary>
    /// Gets the name of this topic as declared in Kafka.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Gets the number of partitions for this topic.
    /// </summary>
    public int Partitions { get; private set; } = 1;

    /// <summary>
    /// Gets the replication factor for this topic.
    /// </summary>
    public short ReplicationFactor { get; private set; } = 1;

    /// <summary>
    /// Gets a value indicating whether this topic is automatically provisioned during topology setup.
    /// When <c>null</c>, the transport-level default is used.
    /// </summary>
    public bool? AutoProvision { get; private set; }

    /// <summary>
    /// Gets the additional topic-level configuration entries (e.g., retention.ms, cleanup.policy).
    /// </summary>
    public Dictionary<string, string>? TopicConfigs { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this is a temporary topic (e.g., a reply topic).
    /// </summary>
    public bool IsTemporary { get; private set; }

    /// <inheritdoc />
    protected override void OnInitialize(TopologyConfiguration configuration)
    {
        var config = (KafkaTopicConfiguration)configuration;

        Name = config.Name;
        Partitions = config.Partitions ?? 1;
        ReplicationFactor = config.ReplicationFactor ?? 1;
        AutoProvision = config.AutoProvision;
        TopicConfigs = config.TopicConfigs;
        IsTemporary = config.IsTemporary;

        Topology = config.Topology!;
        Address = new Uri(Topology.Address, $"t/{Name}");
    }
}
