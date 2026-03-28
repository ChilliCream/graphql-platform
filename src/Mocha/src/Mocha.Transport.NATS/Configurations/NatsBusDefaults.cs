namespace Mocha.Transport.NATS;

/// <summary>
/// Defines bus-level defaults that are applied to all auto-provisioned streams and consumers
/// when they are created by topology conventions.
/// </summary>
public sealed class NatsBusDefaults
{
    /// <summary>
    /// Gets or sets the default stream configuration that is applied to all auto-provisioned streams.
    /// Individual stream settings will override these defaults.
    /// </summary>
    public NatsDefaultStreamOptions Stream { get; set; } = new();

    /// <summary>
    /// Gets or sets the default consumer configuration that is applied to all auto-provisioned consumers.
    /// Individual consumer settings will override these defaults.
    /// </summary>
    public NatsDefaultConsumerOptions Consumer { get; set; } = new();
}

/// <summary>
/// Default options for streams created by topology conventions.
/// </summary>
public sealed class NatsDefaultStreamOptions
{
    /// <summary>
    /// Gets or sets the default maximum number of messages per stream.
    /// When <c>null</c>, uses the JetStream server default (unlimited).
    /// </summary>
    public long? MaxMsgs { get; set; }

    /// <summary>
    /// Gets or sets the default maximum total size in bytes per stream.
    /// When <c>null</c>, uses the JetStream server default (unlimited).
    /// </summary>
    public long? MaxBytes { get; set; }

    /// <summary>
    /// Gets or sets the default maximum age of messages in a stream.
    /// When <c>null</c>, uses the JetStream server default (unlimited).
    /// </summary>
    public TimeSpan? MaxAge { get; set; }

    /// <summary>
    /// Gets or sets the default number of replicas for streams in a NATS JetStream cluster.
    /// When <c>null</c>, uses the JetStream server default (1).
    /// </summary>
    public int? Replicas { get; set; }

    /// <summary>
    /// Applies these defaults to a stream configuration, without overriding explicitly set values.
    /// </summary>
    internal void ApplyTo(NatsStreamConfiguration configuration)
    {
        configuration.MaxMsgs ??= MaxMsgs;
        configuration.MaxBytes ??= MaxBytes;
        configuration.MaxAge ??= MaxAge;
        configuration.Replicas ??= Replicas;
    }
}

/// <summary>
/// Default options for consumers created by topology conventions.
/// </summary>
public sealed class NatsDefaultConsumerOptions
{
    /// <summary>
    /// Gets or sets the default maximum number of unacknowledged messages.
    /// When <c>null</c>, uses the JetStream server default.
    /// </summary>
    public int? MaxAckPending { get; set; }

    /// <summary>
    /// Gets or sets the default acknowledgment wait timeout.
    /// When <c>null</c>, uses the JetStream server default (30 seconds).
    /// </summary>
    public TimeSpan? AckWait { get; set; }

    /// <summary>
    /// Gets or sets the default maximum number of delivery attempts before the message is terminated.
    /// When <c>null</c>, defaults to 5 during provisioning.
    /// </summary>
    public int? MaxDeliver { get; set; }

    /// <summary>
    /// Applies these defaults to a consumer configuration, without overriding explicitly set values.
    /// </summary>
    internal void ApplyTo(NatsConsumerConfiguration configuration)
    {
        configuration.MaxAckPending ??= MaxAckPending;
        configuration.AckWait ??= AckWait;
        configuration.MaxDeliver ??= MaxDeliver;
    }
}
