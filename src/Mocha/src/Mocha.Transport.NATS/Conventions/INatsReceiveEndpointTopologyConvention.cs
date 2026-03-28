namespace Mocha.Transport.NATS;

/// <summary>
/// Convention interface for discovering and provisioning NATS topology resources
/// (streams, subjects, consumers) required by a receive endpoint.
/// </summary>
public interface INatsReceiveEndpointTopologyConvention
    : IReceiveEndpointTopologyConvention<NatsReceiveEndpoint, NatsReceiveEndpointConfiguration>;
