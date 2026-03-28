namespace Mocha.Transport.NATS;

/// <summary>
/// Configuration for a NATS JetStream stream.
/// </summary>
public sealed class NatsStreamConfiguration : TopologyConfiguration<NatsMessagingTopology>
{
    /// <summary>
    /// Gets or sets the name of the stream.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subjects that this stream captures.
    /// Messages published to any of these subjects will be stored in this stream.
    /// </summary>
    public List<string> Subjects { get; set; } = [];

    /// <summary>
    /// Gets or sets the maximum number of messages in the stream.
    /// When <c>null</c>, uses the JetStream server default (unlimited).
    /// </summary>
    public long? MaxMsgs { get; set; }

    /// <summary>
    /// Gets or sets the maximum total size in bytes of the stream.
    /// When <c>null</c>, uses the JetStream server default (unlimited).
    /// </summary>
    public long? MaxBytes { get; set; }

    /// <summary>
    /// Gets or sets the maximum age of messages in the stream.
    /// When <c>null</c>, uses the JetStream server default (unlimited).
    /// </summary>
    public TimeSpan? MaxAge { get; set; }

    /// <summary>
    /// Gets or sets the number of replicas for this stream in a NATS JetStream cluster.
    /// When <c>null</c>, uses the JetStream server default (1).
    /// </summary>
    public int? Replicas { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the stream should be automatically provisioned.
    /// When <c>null</c>, the transport-level default is used.
    /// </summary>
    public bool? AutoProvision { get; set; }
}
