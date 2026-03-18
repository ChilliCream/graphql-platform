namespace Mocha.Transport.InMemory;

/// <summary>
/// Configuration for a receive endpoint that consumes from an in-memory queue.
/// </summary>
public sealed class InMemoryReceiveEndpointConfiguration : ReceiveEndpointConfiguration
{
    /// <summary>
    /// Gets or sets the name of the queue this endpoint will consume from.
    /// </summary>
    public string? QueueName { get; set; }
}
