namespace Mocha;

/// <summary>
/// Base class for transport-specific topologies that manage the physical resources (queues, exchanges, topics) backing endpoints.
/// </summary>
/// <param name="transport">The transport that owns this topology.</param>
/// <param name="baseAddress">The base address URI for this topology.</param>
public abstract class MessagingTopology(MessagingTransport transport, Uri baseAddress)
{
    /// <summary>
    /// Gets the base address URI for this topology.
    /// </summary>
    public Uri Address => baseAddress;

    /// <summary>
    /// Gets the transport that owns this topology.
    /// </summary>
    protected MessagingTransport Transport => transport;
}
