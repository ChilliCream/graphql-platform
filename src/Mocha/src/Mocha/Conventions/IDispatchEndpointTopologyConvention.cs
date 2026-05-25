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

/// <summary>
/// A typed dispatch endpoint topology convention that applies only to specific endpoint and configuration types.
/// </summary>
/// <typeparam name="TEndpoint">The specific dispatch endpoint type.</typeparam>
/// <typeparam name="TConfiguration">The specific dispatch endpoint configuration type.</typeparam>
public interface IDispatchEndpointTopologyConvention<in TEndpoint, in TConfiguration>
    : IDispatchEndpointTopologyConvention
    where TEndpoint : DispatchEndpoint
    where TConfiguration : DispatchEndpointConfiguration
{
    void IDispatchEndpointTopologyConvention.DiscoverTopology(
        IMessagingConfigurationContext context,
        DispatchEndpoint endpoint,
        DispatchEndpointConfiguration configuration)
    {
        if (endpoint is not TEndpoint endpointOfT)
        {
            return;
        }

        if (configuration is not TConfiguration configurationOfT)
        {
            return;
        }

        DiscoverTopology(context, endpointOfT, configurationOfT);
    }

    /// <summary>
    /// Discovers and applies topology for the specified dispatch endpoint and configuration.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="endpoint">The typed dispatch endpoint.</param>
    /// <param name="configuration">The typed dispatch endpoint configuration.</param>
    void DiscoverTopology(IMessagingConfigurationContext context, TEndpoint endpoint, TConfiguration configuration);
}
