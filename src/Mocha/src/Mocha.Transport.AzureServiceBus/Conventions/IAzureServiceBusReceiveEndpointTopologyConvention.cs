namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Convention interface for discovering and provisioning Azure Service Bus topology resources
/// (queues, topics, subscriptions) required by a receive endpoint.
/// </summary>
public interface IAzureServiceBusReceiveEndpointTopologyConvention
    : IReceiveEndpointTopologyConvention<AzureServiceBusReceiveEndpoint, AzureServiceBusReceiveEndpointConfiguration>;
