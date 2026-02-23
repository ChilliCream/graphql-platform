namespace Mocha.Transport.InMemory;

/// <summary>
/// Configuration for a InMemory exchange.
/// </summary>
public sealed class InMemoryTopicConfiguration : TopologyConfiguration
{
    /// <summary>
    /// Gets or sets the name of the exchange.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
