namespace Mocha.Transport.Kafka;

/// <summary>
/// Configuration for a Kafka receive endpoint, specifying the source topic and consumer group settings.
/// </summary>
public sealed class KafkaReceiveEndpointConfiguration : ReceiveEndpointConfiguration
{
    /// <summary>
    /// Gets or sets the Kafka topic name from which this endpoint consumes messages.
    /// </summary>
    public string? TopicName { get; set; }

    /// <summary>
    /// Gets or sets the consumer group ID for this endpoint.
    /// </summary>
    public string? ConsumerGroupId { get; set; }
}
