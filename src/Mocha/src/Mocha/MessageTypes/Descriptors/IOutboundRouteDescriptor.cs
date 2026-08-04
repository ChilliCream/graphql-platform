namespace Mocha;

/// <summary>
/// Provides a fluent API for configuring an outbound message route destination.
/// </summary>
public interface IOutboundRouteDescriptor
{
    /// <summary>
    /// Sets the destination URI for the outbound route.
    /// </summary>
    /// <param name="destination">The destination URI.</param>
    /// <returns>This descriptor for method chaining.</returns>
    IOutboundRouteDescriptor Destination(Uri destination);
}
