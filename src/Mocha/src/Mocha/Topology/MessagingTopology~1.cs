namespace Mocha;

/// <summary>
/// Strongly-typed base class for transport-specific topologies that provides access to the concrete transport type.
/// </summary>
/// <typeparam name="T">The concrete messaging transport type.</typeparam>
/// <param name="transport">The transport that owns this topology.</param>
/// <param name="baseAddress">The base address URI for this topology.</param>
public abstract class MessagingTopology<T>(MessagingTransport transport, Uri baseAddress)
    : MessagingTopology(transport, baseAddress) where T : MessagingTransport
{
    protected new T Transport => (T)base.Transport;
}
