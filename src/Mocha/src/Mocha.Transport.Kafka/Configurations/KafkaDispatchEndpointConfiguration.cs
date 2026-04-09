namespace Mocha.Transport.Kafka;

/// <summary>
/// Configuration for a Kafka dispatch endpoint, specifying the target topic for outbound messages.
/// </summary>
public sealed class KafkaDispatchEndpointConfiguration : DispatchEndpointConfiguration
{
    /// <summary>
    /// Gets or sets the target topic name for message dispatch.
    /// </summary>
    public string? TopicName { get; set; }
}
