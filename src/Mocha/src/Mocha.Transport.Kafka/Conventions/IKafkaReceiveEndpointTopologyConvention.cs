namespace Mocha.Transport.Kafka;

/// <summary>
/// Convention interface for discovering and provisioning Kafka topology resources
/// (topics) required by a receive endpoint.
/// </summary>
public interface IKafkaReceiveEndpointTopologyConvention
    : IReceiveEndpointTopologyConvention<KafkaReceiveEndpoint, KafkaReceiveEndpointConfiguration>;
