using NATS.Client.Core;

namespace HotChocolate.Fusion.Subscriptions.NATS;

/// <summary>
/// Configures a NATS event stream broker.
/// </summary>
public sealed class NatsEventStreamOptions
{
    /// <summary>
    /// Gets or sets the NATS server URL.
    /// </summary>
    /// <remarks>
    /// Either <see cref="Url"/> or <see cref="ConfigureConnection"/> must be configured.
    /// </remarks>
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets a callback that can customize the NATS connection options.
    /// </summary>
    /// <remarks>
    /// The callback receives the options built from the configured values and must return the
    /// options that should be used to create the NATS connection.
    /// </remarks>
    public Func<NatsOpts, NatsOpts>? ConfigureConnection { get; set; }

    /// <summary>
    /// Gets or sets the JetStream options for durable event consumption.
    /// </summary>
    /// <remarks>
    /// When this property is <c>null</c>, the broker uses core NATS pub/sub.
    /// </remarks>
    public NatsJetStreamOptions? JetStream { get; set; }
}

/// <summary>
/// Configures JetStream consumption for a NATS event stream broker.
/// </summary>
public sealed class NatsJetStreamOptions
{
    /// <summary>
    /// Gets or sets the JetStream stream name.
    /// </summary>
    public required string Stream { get; set; }

    /// <summary>
    /// Gets or sets the durable consumer name.
    /// </summary>
    public required string DurableConsumer { get; set; }
}
