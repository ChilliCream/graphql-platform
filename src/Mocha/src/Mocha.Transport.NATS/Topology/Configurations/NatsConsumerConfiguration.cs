namespace Mocha.Transport.NATS;

/// <summary>
/// Configuration for a NATS JetStream durable consumer.
/// </summary>
public sealed class NatsConsumerConfiguration : TopologyConfiguration<NatsMessagingTopology>
{
    /// <summary>
    /// Gets or sets the durable consumer name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the stream this consumer is bound to.
    /// </summary>
    public string? StreamName { get; set; }

    /// <summary>
    /// Gets or sets the filter subject for this consumer.
    /// When set, the consumer only receives messages matching this subject pattern.
    /// </summary>
    public string? FilterSubject { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of unacknowledged messages.
    /// When <c>null</c>, uses the JetStream server default.
    /// </summary>
    public int? MaxAckPending { get; set; }

    /// <summary>
    /// Gets or sets the acknowledgment wait timeout.
    /// When <c>null</c>, uses the JetStream server default (30 seconds).
    /// </summary>
    public TimeSpan? AckWait { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of delivery attempts before the message is terminated.
    /// When <c>null</c>, defaults to 5 during provisioning.
    /// </summary>
    public int? MaxDeliver { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the consumer should be automatically provisioned.
    /// When <c>null</c>, the transport-level default is used.
    /// </summary>
    public bool? AutoProvision { get; set; }
}
