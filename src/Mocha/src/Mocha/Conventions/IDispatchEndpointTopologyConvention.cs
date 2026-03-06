namespace Mocha;

/// <summary>
/// A convention that discovers and configures topology resources for dispatch endpoints during bus setup.
/// </summary>
public interface IDispatchEndpointTopologyConvention : IConvention
{
    /// <summary>
    /// Discovers and configures topology resources (e.g., exchanges, topics) for the specified dispatch endpoint.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="endpoint">The dispatch endpoint to discover topology for.</param>
    /// <param name="configuration">The dispatch endpoint configuration.</param>
    void DiscoverTopology(
        IMessagingConfigurationContext context,
        DispatchEndpoint endpoint,
        DispatchEndpointConfiguration configuration);
}
