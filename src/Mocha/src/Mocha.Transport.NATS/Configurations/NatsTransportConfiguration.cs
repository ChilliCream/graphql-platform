using NATS.Client.Core;

namespace Mocha.Transport.NATS;

/// <summary>
/// Configuration for a NATS JetStream messaging transport, extending the base transport configuration
/// with NATS-specific connection and stream settings.
/// </summary>
public class NatsTransportConfiguration : MessagingTransportConfiguration
{
    /// <summary>
    /// The default transport name used when no explicit name is specified.
    /// </summary>
    public const string DefaultName = "nats";

    /// <summary>
    /// The default URI schema used for NATS transport addresses.
    /// </summary>
    public const string DefaultSchema = "nats";

    /// <summary>
    /// Creates a new configuration instance with the default name and schema.
    /// </summary>
    public NatsTransportConfiguration()
    {
        Name = DefaultName;
        Schema = DefaultSchema;
    }

    /// <summary>
    /// Gets or sets the NATS server URL (e.g., "nats://localhost:4222").
    /// When <c>null</c>, defaults to "nats://localhost:4222".
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets a factory delegate that resolves a <see cref="NatsConnection"/> from the service provider.
    /// </summary>
    /// <remarks>
    /// When <c>null</c>, the transport falls back to resolving a <see cref="NatsConnection"/> from DI,
    /// or creates a new one using the configured <see cref="Url"/>.
    /// </remarks>
    public Func<IServiceProvider, NatsConnection>? ConnectionFactory { get; set; }

    /// <summary>
    /// Gets or sets the explicitly declared streams for this transport.
    /// </summary>
    public List<NatsStreamConfiguration> Streams { get; set; } = [];

    /// <summary>
    /// Gets or sets the explicitly declared consumers for this transport.
    /// </summary>
    public List<NatsConsumerConfiguration> Consumers { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether topology resources (streams, consumers)
    /// should be automatically provisioned on the broker. When <c>null</c>, defaults to <c>true</c>.
    /// Individual resources can override this setting.
    /// </summary>
    public bool? AutoProvision { get; set; }

    /// <summary>
    /// Gets or sets the bus-level defaults applied to all auto-provisioned streams and consumers.
    /// </summary>
    public NatsBusDefaults Defaults { get; set; } = new();
}
