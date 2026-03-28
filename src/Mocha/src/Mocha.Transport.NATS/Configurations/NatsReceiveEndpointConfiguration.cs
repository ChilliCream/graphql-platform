namespace Mocha.Transport.NATS;

/// <summary>
/// Configuration for a NATS receive endpoint, specifying the source subject, consumer, and prefetch settings.
/// </summary>
public sealed class NatsReceiveEndpointConfiguration : ReceiveEndpointConfiguration
{
    /// <summary>
    /// Gets or sets the NATS subject name from which this endpoint consumes messages.
    /// </summary>
    public string? SubjectName { get; set; }

    /// <summary>
    /// Gets or sets the JetStream durable consumer name for this endpoint.
    /// </summary>
    public string? ConsumerName { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of unacknowledged messages the consumer will receive.
    /// Maps to JetStream consumer <c>MaxAckPending</c>. Defaults to 100.
    /// </summary>
    public int MaxPrefetch { get; set; } = 100;
}
