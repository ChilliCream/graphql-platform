namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Convention interface for applying RabbitMQ-specific configuration to receive endpoints.
/// Implementations receive the narrowed <see cref="RabbitMQReceiveEndpointConfiguration"/> type.
/// </summary>
public interface IRabbitMQReceiveEndpointConfigurationConvention : IReceiveEndpointConvention
{
    /// <inheritdoc />
    void IConfigurationConvention<ReceiveEndpointConfiguration>.Configure(
        IMessagingConfigurationContext context,
        ReceiveEndpointConfiguration configuration)
    {
        if (configuration is not RabbitMQReceiveEndpointConfiguration rabbitMQConfiguration)
        {
            return;
        }

        Configure(context, rabbitMQConfiguration);
    }

    /// <summary>
    /// Applies RabbitMQ-specific configuration to the given receive endpoint configuration.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="configuration">The RabbitMQ receive endpoint configuration to modify.</param>
    void Configure(IMessagingConfigurationContext context, RabbitMQReceiveEndpointConfiguration configuration);
}
