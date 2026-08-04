namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Convention interface for applying RabbitMQ-specific configuration to receive endpoints.
/// Implementations receive the narrowed <see cref="RabbitMQReceiveEndpointConfiguration"/> type.
/// </summary>
public interface IRabbitMQReceiveEndpointConfigurationConvention
    : IEndpointConfigurationConvention<ReceiveEndpointConfiguration>
{
    void IEndpointConfigurationConvention<ReceiveEndpointConfiguration>.Configure(
        IMessagingConfigurationContext context,
        MessagingTransport transport,
        ReceiveEndpointConfiguration configuration)
    {
        if (configuration is not RabbitMQReceiveEndpointConfiguration rabbitMQConfiguration)
        {
            return;
        }

        if (transport is not RabbitMQMessagingTransport rabbitMQTransport)
        {
            return;
        }

        Configure(context, rabbitMQTransport, rabbitMQConfiguration);
    }

    /// <summary>
    /// Applies RabbitMQ-specific configuration to the given receive endpoint configuration.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="transport">The RabbitMQ messaging transport instance.</param>
    /// <param name="configuration">The RabbitMQ receive endpoint configuration to modify.</param>
    void Configure(
        IMessagingConfigurationContext context,
        RabbitMQMessagingTransport transport,
        RabbitMQReceiveEndpointConfiguration configuration);
}
