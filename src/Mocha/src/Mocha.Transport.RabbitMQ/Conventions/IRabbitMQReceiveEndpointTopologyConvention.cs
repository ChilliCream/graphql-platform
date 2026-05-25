namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Convention interface for discovering and provisioning RabbitMQ topology resources
/// (queues, exchanges, bindings) required by a receive endpoint.
/// </summary>
public interface IRabbitMQReceiveEndpointTopologyConvention
    : IReceiveEndpointTopologyConvention<RabbitMQReceiveEndpoint, RabbitMQReceiveEndpointConfiguration>;
