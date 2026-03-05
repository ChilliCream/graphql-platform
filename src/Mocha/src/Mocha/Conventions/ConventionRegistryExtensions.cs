namespace Mocha;

/// <summary>
/// Provides extension methods on <see cref="IConventionRegistry"/> for applying configuration and
/// topology conventions.
/// </summary>
public static class ConventionRegistryExtensions
{
    /// <summary>
    /// Applies all registered <see cref="IConfigurationConvention{T}"/> conventions to the
    /// specified configuration.
    /// </summary>
    /// <typeparam name="T">The configuration type.</typeparam>
    /// <param name="registry">The convention registry.</param>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="configuration">The configuration to apply conventions to.</param>
    public static void Configure<T>(
        this IConventionRegistry registry,
        IMessagingConfigurationContext context,
        T configuration)
        where T : MessagingConfiguration
    {
        foreach (var convention in registry.GetConventions<IConfigurationConvention<T>>())
        {
            convention.Configure(context, configuration);
        }
    }

    /// <summary>
    /// Applies all registered <see cref="IReceiveEndpointTopologyConvention"/> conventions to
    /// discover topology for a receive endpoint.
    /// </summary>
    /// <param name="registry">The convention registry.</param>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="endpoint">The receive endpoint to discover topology for.</param>
    /// <param name="configuration">The receive endpoint configuration.</param>
    public static void DiscoverTopology(
        this IConventionRegistry registry,
        IMessagingConfigurationContext context,
        ReceiveEndpoint endpoint,
        ReceiveEndpointConfiguration configuration)
    {
        foreach (var convention in registry.GetConventions<IReceiveEndpointTopologyConvention>())
        {
            convention.DiscoverTopology(context, endpoint, configuration);
        }
    }

    /// <summary>
    /// Applies all registered <see cref="IDispatchEndpointTopologyConvention"/> conventions to
    /// discover topology for a dispatch endpoint.
    /// </summary>
    /// <param name="registry">The convention registry.</param>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="endpoint">The dispatch endpoint to discover topology for.</param>
    /// <param name="configuration">The dispatch endpoint configuration.</param>
    public static void DiscoverTopology(
        this IConventionRegistry registry,
        IMessagingConfigurationContext context,
        DispatchEndpoint endpoint,
        DispatchEndpointConfiguration configuration)
    {
        foreach (var convention in registry.GetConventions<IDispatchEndpointTopologyConvention>())
        {
            convention.DiscoverTopology(context, endpoint, configuration);
        }
    }
}
