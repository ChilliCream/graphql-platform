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

/// <summary>
/// A typed receive endpoint topology convention that applies only to specific endpoint and configuration types.
/// </summary>
/// <typeparam name="TEndpoint">The specific receive endpoint type.</typeparam>
/// <typeparam name="TConfiguration">The specific receive endpoint configuration type.</typeparam>
public interface IReceiveEndpointTopologyConvention<in TEndpoint, in TConfiguration>
    : IReceiveEndpointTopologyConvention
    where TEndpoint : ReceiveEndpoint
    where TConfiguration : ReceiveEndpointConfiguration
{
    void IReceiveEndpointTopologyConvention.DiscoverTopology(
        IMessagingConfigurationContext context,
        ReceiveEndpoint endpoint,
        ReceiveEndpointConfiguration configuration)
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
    /// Discovers and applies topology for the specified receive endpoint and configuration.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="endpoint">The typed receive endpoint.</param>
    /// <param name="configuration">The typed receive endpoint configuration.</param>
    void DiscoverTopology(IMessagingConfigurationContext context, TEndpoint endpoint, TConfiguration configuration);
}
