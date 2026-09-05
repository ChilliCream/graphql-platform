namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Configuration for a source topic binding declared from a queue descriptor.
/// </summary>
public sealed class AzureServiceBusQueueSourceTopicConfiguration
{
    /// <summary>
    /// Gets or sets the source topic address.
    /// </summary>
    public Uri Source { get; set; } = null!;

    /// <summary>
    /// Gets or sets whether the derived subscription is provisioned. <c>null</c> inherits the
    /// queue's setting, falling back to the transport default.
    /// </summary>
    public bool? AutoProvision { get; set; }
}
