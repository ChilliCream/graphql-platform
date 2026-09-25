namespace Mocha.Transport.Nats;

/// <summary>
/// NATS JetStream specific transport configuration, holding the stream and consumer declarations
/// collected by the descriptor during bus setup.
/// </summary>
public sealed class NatsTransportConfiguration : MessagingTransportConfiguration
{
    /// <summary>
    /// The default transport name.
    /// </summary>
    public const string DefaultName = "nats";

    /// <summary>
    /// The default URI scheme.
    /// </summary>
    public const string DefaultSchema = "nats";

    /// <summary>
    /// Creates a configuration with the transport defaults applied.
    /// </summary>
    public NatsTransportConfiguration()
    {
        Name = DefaultName;
        Schema = DefaultSchema;
    }

    /// <summary>
    /// Gets or sets a value indicating whether streams and consumers are created on start-up.
    /// When <see langword="null"/> the transport default of <see langword="true"/> applies.
    /// </summary>
    public bool? AutoProvision { get; set; }

    /// <summary>
    /// Gets or sets a factory delegate that resolves an <see cref="INatsConnectionProvider"/> from
    /// the service provider.
    /// </summary>
    /// <remarks>
    /// When <see langword="null"/>, the transport resolves an <see cref="NATS.Client.Core.INatsConnection"/>
    /// from dependency injection and wraps it in a <see cref="NatsConnectionProvider"/>.
    /// </remarks>
    public Func<IServiceProvider, INatsConnectionProvider>? ConnectionProvider { get; set; }

    /// <summary>
    /// Gets or sets the name of the stream capturing the subjects nothing else has claimed, defaulting
    /// to the host's service name.
    /// </summary>
    public string? StreamName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the convention stream supports scheduled and expiring
    /// messages.
    /// </summary>
    /// <remarks>
    /// Enabling this turns on per-message TTL and message schedules on the stream, and captures an
    /// extra scheduling subject alongside each subject. The scheduling subject is required because
    /// the server refuses a schedule whose target subject is the one it was published to.
    /// </remarks>
    public bool EnableScheduling { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a publish carries the header JetStream deduplicates on.
    /// </summary>
    /// <remarks>
    /// Off by default. The stream discards a repeated identifier within its deduplication window
    /// without reporting an error, which suppresses a deliberate republish of the same message as
    /// well as an accidental one.
    /// </remarks>
    public bool EnablePublishDeduplication { get; set; }

    /// <summary>
    /// Gets or sets the bus-level defaults applied to streams and consumers created by conventions.
    /// </summary>
    public NatsBusDefaults Defaults { get; set; } = new();

    /// <summary>
    /// Gets or sets the explicitly declared streams for this transport.
    /// </summary>
    public List<NatsStreamConfiguration> Streams { get; set; } = [];

    /// <summary>
    /// Gets or sets the explicitly declared consumers for this transport.
    /// </summary>
    public List<NatsConsumerConfiguration> Consumers { get; set; } = [];
}
