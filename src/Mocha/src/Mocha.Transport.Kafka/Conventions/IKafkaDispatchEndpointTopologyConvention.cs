namespace Mocha.Transport.Kafka;

/// <summary>
/// Convention interface for discovering and provisioning Kafka topology resources
/// (topics) required by a dispatch endpoint.
/// </summary>
public interface IKafkaDispatchEndpointTopologyConvention
    : IDispatchEndpointTopologyConvention<KafkaDispatchEndpoint, KafkaDispatchEndpointConfiguration>;
