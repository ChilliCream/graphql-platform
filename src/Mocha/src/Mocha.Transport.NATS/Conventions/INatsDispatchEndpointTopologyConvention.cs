namespace Mocha.Transport.NATS;

/// <summary>
/// Convention interface for discovering and provisioning NATS topology resources
/// (streams, subjects) required by a dispatch endpoint.
/// </summary>
public interface INatsDispatchEndpointTopologyConvention
    : IDispatchEndpointTopologyConvention<NatsDispatchEndpoint, NatsDispatchEndpointConfiguration>;
