namespace Mocha.Transport.InMemory;

/// <summary>
/// Configuration for a InMemory binding that connects an exchange to a queue or another exchange.
/// </summary>
public sealed class InMemoryBindingConfiguration : TopologyConfiguration
{
    /// <summary>
    /// Gets or sets the name of the source exchange.
    /// This is the exchange from which messages will be routed.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the destination queue or exchange.
    /// This is where messages will be routed to based on the binding rules.
    /// </summary>
    public string Destination { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the destination is a queue or a topic.
    /// </summary>
    public InMemoryDestinationKind DestinationKind { get; set; }
}

/// <summary>
/// Specifies whether a binding destination is a queue or a topic.
/// </summary>
public enum InMemoryDestinationKind
{
    /// <summary>
    /// The binding destination is a queue.
    /// </summary>
    Queue,

    /// <summary>
    /// The binding destination is a topic.
    /// </summary>
    Topic
}
