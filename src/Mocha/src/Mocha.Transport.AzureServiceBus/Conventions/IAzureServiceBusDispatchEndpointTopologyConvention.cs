namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Convention interface for discovering and provisioning Azure Service Bus topology resources
/// (topics, queues) required by a dispatch endpoint.
/// </summary>
public interface IAzureServiceBusDispatchEndpointTopologyConvention
    : IDispatchEndpointTopologyConvention<AzureServiceBusDispatchEndpoint, AzureServiceBusDispatchEndpointConfiguration>;
