namespace Mocha.Transport.Nats;

/// <summary>
/// Configuration for a dispatch endpoint publishing to a JetStream subject.
/// </summary>
public sealed class NatsDispatchEndpointConfiguration : DispatchEndpointConfiguration
{
    /// <summary>
    /// Gets or sets the subject this endpoint publishes to.
    /// </summary>
    public string? Subject { get; set; }
}
