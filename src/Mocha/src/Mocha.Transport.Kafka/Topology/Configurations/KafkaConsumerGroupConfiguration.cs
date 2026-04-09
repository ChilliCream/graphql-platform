namespace Mocha.Transport.Kafka;

/// <summary>
/// Configuration for a Kafka consumer group.
/// </summary>
public sealed class KafkaConsumerGroupConfiguration : TopologyConfiguration<KafkaMessagingTopology>
{
    /// <summary>
    /// Gets or sets the consumer group identifier.
    /// </summary>
    public string GroupId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the topic that this consumer group subscribes to.
    /// </summary>
    public KafkaTopic? Topic { get; set; }
}
