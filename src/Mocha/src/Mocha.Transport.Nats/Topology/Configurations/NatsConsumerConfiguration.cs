using NATS.Client.JetStream.Models;

namespace Mocha.Transport.Nats;

/// <summary>
/// Configuration for a durable JetStream pull consumer.
/// </summary>
public sealed class NatsConsumerConfiguration : TopologyConfiguration<NatsMessagingTopology>
{
    /// <summary>
    /// Gets or sets the durable consumer name, for example <c>order-service_order-created</c>.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the name of the stream this consumer reads from.
    /// </summary>
    /// <remarks>
    /// When left unset the transport resolves the owning stream from the consumer's filter subjects
    /// at start-up, because the stream capturing a subject belongs to the publishing service.
    /// </remarks>
    public string? StreamName { get; set; }

    /// <summary>
    /// Gets or sets the subjects this consumer receives, mapping Mocha's binding list.
    /// </summary>
    public IList<string>? FilterSubjects { get; set; }

    /// <summary>
    /// Gets or sets how long the server waits for an acknowledgement before redelivering.
    /// </summary>
    public TimeSpan? AckWait { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of delivery attempts.
    /// </summary>
    /// <remarks>
    /// Mocha owns the retry decision, so this acts as a safety net above Mocha's own retry policy
    /// rather than as the primary limit.
    /// </remarks>
    public long? MaxDeliver { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of unacknowledged messages in flight, which is how this
    /// transport applies back pressure instead of a prefetch count.
    /// </summary>
    public long? MaxAckPending { get; set; }

    /// <summary>
    /// Gets or sets the per-attempt redelivery backoff curve.
    /// </summary>
    public IList<TimeSpan>? Backoff { get; set; }

    /// <summary>
    /// Gets or sets how often an in-flight message reports progress to extend its acknowledgement
    /// deadline, or <see langword="null"/> to never report progress.
    /// </summary>
    /// <remarks>
    /// Set this for handlers that can legitimately run longer than <see cref="AckWait"/>, which
    /// would otherwise be redelivered while still working.
    /// </remarks>
    public TimeSpan? AckProgressInterval { get; set; }

    /// <summary>
    /// Gets or sets where a newly created consumer starts reading.
    /// </summary>
    public ConsumerConfigDeliverPolicy? DeliverPolicy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the consumer is provisioned during topology setup.
    /// </summary>
    public bool? AutoProvision { get; set; }
}
