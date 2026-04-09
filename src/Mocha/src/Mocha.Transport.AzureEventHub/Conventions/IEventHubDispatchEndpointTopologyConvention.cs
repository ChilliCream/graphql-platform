namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Convention interface for discovering and provisioning Event Hub topology resources
/// (topics) required by a dispatch endpoint.
/// </summary>
public interface IEventHubDispatchEndpointTopologyConvention
    : IDispatchEndpointTopologyConvention<EventHubDispatchEndpoint, EventHubDispatchEndpointConfiguration>;
