using NATS.Client.JetStream.Models;

namespace Mocha.Transport.Nats;

/// <summary>
/// Default options for durable consumers created by topology conventions.
/// </summary>
public sealed class NatsDefaultConsumerOptions
{
    /// <summary>
    /// Gets or sets how long the server waits by default for an acknowledgement before redelivering.
    /// When left unset the server applies its own acknowledgement deadline.
    /// </summary>
    public TimeSpan? AckWait { get; set; }

    /// <summary>
    /// Gets or sets the default maximum number of delivery attempts. When left unset the server
    /// applies no limit beyond Mocha's own retry policy.
    /// </summary>
    public long? MaxDeliver { get; set; }

    /// <summary>
    /// Gets or sets the default maximum number of unacknowledged messages in flight. When left unset
    /// consumers use <see cref="NatsConsumer.DefaultMaxAckPending"/>.
    /// </summary>
    public long? MaxAckPending { get; set; }

    /// <summary>
    /// Gets or sets the default per-attempt redelivery backoff curve. When left unset the server
    /// redelivers on its acknowledgement deadline alone.
    /// </summary>
    public IList<TimeSpan>? Backoff { get; set; }

    /// <summary>
    /// Gets or sets how often an in-flight message reports progress by default.
    /// <see langword="null"/> means progress is never reported.
    /// </summary>
    public TimeSpan? AckProgressInterval { get; set; }

    /// <summary>
    /// Gets or sets where a newly created consumer starts reading. When left unset consumers use
    /// <see cref="NatsConsumer.DefaultDeliverPolicy"/>.
    /// </summary>
    public ConsumerConfigDeliverPolicy? DeliverPolicy { get; set; }

    /// <summary>
    /// Applies these defaults to a consumer configuration, leaving explicitly set values untouched.
    /// </summary>
    internal void ApplyTo(NatsConsumerConfiguration configuration)
    {
        configuration.AckWait ??= AckWait;
        configuration.MaxDeliver ??= MaxDeliver;
        configuration.MaxAckPending ??= MaxAckPending;
        configuration.AckProgressInterval ??= AckProgressInterval;
        configuration.DeliverPolicy ??= DeliverPolicy;

        if (configuration.Backoff is null && Backoff is { Count: > 0 })
        {
            configuration.Backoff = [.. Backoff];
        }
    }
}
