namespace Mocha.Transport.InMemory;

/// <summary>
/// Configuration for a InMemory queue.
/// </summary>
public sealed class InMemoryQueueConfiguration : TopologyConfiguration<InMemoryMessagingTopology>
{
    /// <summary>
    /// Gets or sets the name of the queue.
    /// </summary>
    public string? Name { get; set; }
}
