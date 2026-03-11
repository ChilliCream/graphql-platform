namespace Mocha;

/// <summary>
/// Marker interface for all messaging conventions that customize bus behavior during setup.
/// </summary>
public interface IConvention;

/// <summary>
/// A typed configuration convention that applies only to configurations of type
/// <typeparamref name="TConfiguration"/>.
/// </summary>
/// <typeparam name="TConfiguration">The specific configuration type this convention applies to.</typeparam>
public interface IConfigurationConvention<in TConfiguration> : IConfigurationConvention
{
    void IConfigurationConvention.Configure(
        IMessagingConfigurationContext context,
        MessagingConfiguration configuration)
    {
        if (configuration is not TConfiguration configurationOfT)
        {
            return;
        }

        Configure(context, configurationOfT);
    }

    /// <summary>
    /// Applies convention-based configuration to a configuration object of the specified type.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="configuration">The typed configuration object to modify.</param>
    void Configure(IMessagingConfigurationContext context, TConfiguration configuration);
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
