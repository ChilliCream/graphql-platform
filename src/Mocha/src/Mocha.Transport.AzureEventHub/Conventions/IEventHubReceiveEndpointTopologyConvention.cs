namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Convention interface for discovering and provisioning Event Hub topology resources
/// (topics, subscriptions) required by a receive endpoint.
/// </summary>
public interface IEventHubReceiveEndpointTopologyConvention
    : IReceiveEndpointTopologyConvention<EventHubReceiveEndpoint, EventHubReceiveEndpointConfiguration>;
