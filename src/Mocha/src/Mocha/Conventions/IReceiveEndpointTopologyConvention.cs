namespace Mocha;

/// <summary>
/// A convention that discovers and configures topology resources for receive endpoints during bus setup.
/// </summary>
public interface IReceiveEndpointTopologyConvention : IConvention
{
    /// <summary>
    /// Discovers and configures topology resources (e.g., queues, subscriptions) for the specified receive endpoint.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="endpoint">The receive endpoint to discover topology for.</param>
    /// <param name="configuration">The receive endpoint configuration.</param>
    void DiscoverTopology(
        IMessagingConfigurationContext context,
        ReceiveEndpoint endpoint,
        ReceiveEndpointConfiguration configuration);
}
