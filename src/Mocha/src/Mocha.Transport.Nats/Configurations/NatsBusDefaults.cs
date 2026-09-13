namespace Mocha.Transport.Nats;

/// <summary>
/// Defines bus-level defaults applied to the streams and durable consumers created by topology
/// conventions.
/// </summary>
public sealed class NatsBusDefaults
{
    /// <summary>
    /// Gets or sets the default stream configuration. Settings on an individual stream override
    /// these defaults.
    /// </summary>
    public NatsDefaultStreamOptions Stream { get; set; } = new();

    /// <summary>
    /// Gets or sets the default consumer configuration. Settings on an individual consumer or
    /// endpoint override these defaults.
    /// </summary>
    public NatsDefaultConsumerOptions Consumer { get; set; } = new();
}
