using NATS.Client.JetStream.Models;

namespace Mocha.Transport.Nats;

/// <summary>
/// Default options for streams created by topology conventions.
/// </summary>
public sealed class NatsDefaultStreamOptions
{
    /// <summary>
    /// Gets or sets the default retention policy. When left unset the stream uses
    /// <see cref="StreamConfigRetention.Limits"/>.
    /// </summary>
    public StreamConfigRetention? Retention { get; set; }

    /// <summary>
    /// Gets or sets the default storage backend. When left unset the stream uses
    /// <see cref="StreamConfigStorage.File"/>.
    /// </summary>
    public StreamConfigStorage? Storage { get; set; }

    /// <summary>
    /// Gets or sets how long messages are retained by default. When left unset the server applies
    /// no age limit.
    /// </summary>
    public TimeSpan? MaxAge { get; set; }

    /// <summary>
    /// Gets or sets the default maximum number of messages retained. When left unset the server
    /// applies no message limit.
    /// </summary>
    public long? MaxMsgs { get; set; }

    /// <summary>
    /// Gets or sets the default maximum total size in bytes. When left unset the server applies no
    /// size limit.
    /// </summary>
    public long? MaxBytes { get; set; }

    /// <summary>
    /// Gets or sets the default number of replicas. When left unset the server uses a single replica.
    /// </summary>
    public int? NumReplicas { get; set; }

    /// <summary>
    /// Gets or sets the default window within which a repeated <c>Nats-Msg-Id</c> is treated as a
    /// duplicate. When left unset the server applies its own default, which does not disable
    /// deduplication.
    /// </summary>
    public TimeSpan? DuplicateWindow { get; set; }

    /// <summary>
    /// Applies these defaults to a stream configuration, leaving explicitly set values untouched.
    /// </summary>
    internal void ApplyTo(NatsStreamConfiguration configuration)
    {
        configuration.Retention ??= Retention;
        configuration.Storage ??= Storage;
        configuration.MaxAge ??= MaxAge;
        configuration.MaxMsgs ??= MaxMsgs;
        configuration.MaxBytes ??= MaxBytes;
        configuration.NumReplicas ??= NumReplicas;
        configuration.DuplicateWindow ??= DuplicateWindow;
    }
}
