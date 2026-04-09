namespace Mocha.Transport.Kafka;

/// <summary>
/// Convention interface for applying Kafka-specific configuration to receive endpoints.
/// Implementations receive the narrowed <see cref="KafkaReceiveEndpointConfiguration"/> type.
/// </summary>
public interface IKafkaReceiveEndpointConfigurationConvention
    : IEndpointConfigurationConvention<ReceiveEndpointConfiguration>
{
    void IEndpointConfigurationConvention<ReceiveEndpointConfiguration>.Configure(
        IMessagingConfigurationContext context,
        MessagingTransport transport,
        ReceiveEndpointConfiguration configuration)
    {
        if (configuration is not KafkaReceiveEndpointConfiguration kafkaConfiguration)
        {
            return;
        }

        if (transport is not KafkaMessagingTransport kafkaTransport)
        {
            return;
        }

        Configure(context, kafkaTransport, kafkaConfiguration);
    }

    /// <summary>
    /// Applies Kafka-specific configuration to the given receive endpoint configuration.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="transport">The Kafka messaging transport instance.</param>
    /// <param name="configuration">The Kafka receive endpoint configuration to modify.</param>
    void Configure(
        IMessagingConfigurationContext context,
        KafkaMessagingTransport transport,
        KafkaReceiveEndpointConfiguration configuration);
}
