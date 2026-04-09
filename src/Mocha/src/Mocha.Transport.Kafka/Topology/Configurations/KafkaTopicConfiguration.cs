namespace Mocha.Transport.Kafka;

/// <summary>
/// Configuration for a Kafka topic.
/// </summary>
public sealed class KafkaTopicConfiguration : TopologyConfiguration<KafkaMessagingTopology>
{
    /// <summary>
    /// Gets or sets the name of the topic.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of partitions for the topic.
    /// When <c>null</c>, defaults to the bus-level default or 1.
    /// </summary>
    public int? Partitions { get; set; }

    /// <summary>
    /// Gets or sets the replication factor for the topic.
    /// When <c>null</c>, defaults to the bus-level default or 1.
    /// </summary>
    public short? ReplicationFactor { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the topic should be automatically provisioned.
    /// When <c>null</c>, the transport-level default is used.
    /// </summary>
    public bool? AutoProvision { get; set; }

    /// <summary>
    /// Gets or sets additional topic-level configuration entries (e.g., retention.ms, cleanup.policy).
    /// </summary>
    public Dictionary<string, string>? TopicConfigs { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is a temporary topic (e.g., a reply topic).
    /// Temporary topics may have special retention policies applied.
    /// </summary>
    public bool IsTemporary { get; set; }
}
