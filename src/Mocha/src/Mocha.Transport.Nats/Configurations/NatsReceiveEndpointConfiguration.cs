using NATS.Client.JetStream.Models;

namespace Mocha.Transport.Nats;

/// <summary>
/// Configuration for a receive endpoint consuming from a durable JetStream pull consumer.
/// </summary>
public sealed class NatsReceiveEndpointConfiguration : ReceiveEndpointConfiguration
{
    /// <summary>
    /// Gets or sets the durable consumer name.
    /// </summary>
    public string? ConsumerName { get; set; }

    /// <summary>
    /// Gets or sets the stream this consumer reads from.
    /// </summary>
    /// <remarks>
    /// Left unset for subjects published by another service, where the owning stream is resolved
    /// against the server during start-up.
    /// </remarks>
    public string? StreamName { get; set; }

    /// <summary>
    /// Gets or sets the subjects this endpoint receives.
    /// </summary>
    public List<string> FilterSubjects { get; set; } = [];

    /// <summary>
    /// Gets or sets how long the server waits for an acknowledgement before redelivering.
    /// </summary>
    public TimeSpan? AckWait { get; set; }

    /// <summary>
    /// Gets or sets how many times a message is delivered before it is treated as undeliverable.
    /// </summary>
    public long? MaxDeliver { get; set; }

    /// <summary>
    /// Gets or sets the delays applied before each redelivery.
    /// </summary>
    public List<TimeSpan>? Backoff { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of unacknowledged messages in flight across every instance
    /// reading this consumer.
    /// </summary>
    public long? MaxAckPending { get; set; }

    /// <summary>
    /// Gets or sets how often an in-flight message reports progress to extend its acknowledgement
    /// deadline, or <see langword="null"/> when progress is never reported.
    /// </summary>
    public TimeSpan? AckProgressInterval { get; set; }

    /// <summary>
    /// Gets or sets where the consumer starts reading when it is created.
    /// </summary>
    public ConsumerConfigDeliverPolicy? DeliverPolicy { get; set; }
}
