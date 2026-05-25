namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Convention interface for discovering and provisioning RabbitMQ topology resources
/// (exchanges, queues) required by a dispatch endpoint.
/// </summary>
public interface IRabbitMQDispatchEndpointTopologyConvention
    : IDispatchEndpointTopologyConvention<RabbitMQDispatchEndpoint, RabbitMQDispatchEndpointConfiguration>;
