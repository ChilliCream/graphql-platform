using NATS.Client.JetStream.Models;

namespace Mocha.Transport.Nats;

/// <summary>
/// Configuration for a JetStream stream.
/// </summary>
public sealed class NatsStreamConfiguration : TopologyConfiguration<NatsMessagingTopology>
{
    /// <summary>
    /// Gets or sets the name of the stream, for example <c>ORDER_SERVICE</c>.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the subjects this stream captures, for example <c>order-service.&gt;</c>.
    /// </summary>
    public IList<string>? Subjects { get; set; }

    /// <summary>
    /// Gets or sets the retention policy. Defaults to <see cref="StreamConfigRetention.Limits"/>.
    /// </summary>
    public StreamConfigRetention? Retention { get; set; }

    /// <summary>
    /// Gets or sets the storage backend. Defaults to <see cref="StreamConfigStorage.File"/>.
    /// </summary>
    public StreamConfigStorage? Storage { get; set; }

    /// <summary>
    /// Gets or sets how long messages are retained.
    /// </summary>
    public TimeSpan? MaxAge { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of messages retained.
    /// </summary>
    public long? MaxMsgs { get; set; }

    /// <summary>
    /// Gets or sets the maximum total size of the stream in bytes.
    /// </summary>
    public long? MaxBytes { get; set; }

    /// <summary>
    /// Gets or sets the number of replicas.
    /// </summary>
    public int? NumReplicas { get; set; }

    /// <summary>
    /// Gets or sets the window within which a repeated <c>Nats-Msg-Id</c> is treated as a duplicate.
    /// When left unset the server applies its own default, which does not disable deduplication.
    /// </summary>
    /// <remarks>
    /// Deduplication cannot be turned off from here. A zero window is omitted from the request the
    /// client sends, so the server falls back to its default of two minutes.
    /// </remarks>
    public TimeSpan? DuplicateWindow { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether per-message TTL headers are honoured.
    /// Requires NATS server 2.11 or later.
    /// </summary>
    public bool? AllowMsgTtl { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether message scheduling is enabled.
    /// Requires NATS server 2.12 or later.
    /// </summary>
    public bool? AllowMsgSchedules { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the stream is provisioned during topology setup.
    /// </summary>
    public bool? AutoProvision { get; set; }
}
