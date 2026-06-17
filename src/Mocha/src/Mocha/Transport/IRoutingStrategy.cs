namespace Mocha;

/// <summary>
/// Defines the structural routing model for a transport: how routes map to endpoints and how
/// endpoints map to transport topology resources.
/// </summary>
public interface IRoutingStrategy
{
    /// <summary>
    /// Creates the dispatch endpoint configuration for an outbound route.
    /// </summary>
    DispatchEndpointConfiguration? CreateEndpointConfiguration(
        IMessagingConfigurationContext context,
        OutboundRoute route);

    /// <summary>
    /// Creates the dispatch endpoint configuration for an address.
    /// </summary>
    DispatchEndpointConfiguration? CreateEndpointConfiguration(
        IMessagingConfigurationContext context,
        Uri address);

    /// <summary>
    /// Creates the receive endpoint configuration for an inbound route.
    /// </summary>
    ReceiveEndpointConfiguration? CreateEndpointConfiguration(
        IMessagingConfigurationContext context,
        InboundRoute route);

    /// <summary>
    /// Discovers the receive and dispatch endpoint layout for the transport.
    /// </summary>
    void DiscoverEndpoints(IMessagingSetupContext context);

    /// <summary>
    /// Discovers transport topology resources for a receive endpoint.
    /// </summary>
    void DiscoverTopology(
        IMessagingConfigurationContext context,
        ReceiveEndpoint endpoint,
        ReceiveEndpointConfiguration configuration);

    /// <summary>
    /// Discovers transport topology resources for a dispatch endpoint.
    /// </summary>
    void DiscoverTopology(
        IMessagingConfigurationContext context,
        DispatchEndpoint endpoint,
        DispatchEndpointConfiguration configuration);
}
