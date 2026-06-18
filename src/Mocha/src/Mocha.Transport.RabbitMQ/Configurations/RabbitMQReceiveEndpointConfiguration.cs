namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Configuration for a RabbitMQ receive endpoint, specifying the source queue and consumer prefetch settings.
/// </summary>
public sealed class RabbitMQReceiveEndpointConfiguration : ReceiveEndpointConfiguration
{
    /// <summary>
    /// Gets or sets the RabbitMQ queue name from which this endpoint consumes messages.
    /// </summary>
    public string? QueueName { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of unacknowledged messages the broker will deliver to this endpoint's consumer.
    /// Defaults to 100.
    /// </summary>
    public ushort MaxPrefetch { get; set; } = 100;
}
