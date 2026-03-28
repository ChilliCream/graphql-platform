namespace Mocha.Transport.NATS;

/// <summary>
/// Convention interface for applying NATS-specific configuration to receive endpoints.
/// Implementations receive the narrowed <see cref="NatsReceiveEndpointConfiguration"/> type.
/// </summary>
public interface INatsReceiveEndpointConfigurationConvention : IReceiveEndpointConvention
{
    /// <inheritdoc />
    void IConfigurationConvention<ReceiveEndpointConfiguration>.Configure(
        IMessagingConfigurationContext context,
        ReceiveEndpointConfiguration configuration)
    {
        if (configuration is not NatsReceiveEndpointConfiguration natsConfiguration)
        {
            return;
        }

        Configure(context, natsConfiguration);
    }

    /// <summary>
    /// Applies NATS-specific configuration to the given receive endpoint configuration.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="configuration">The NATS receive endpoint configuration to modify.</param>
    void Configure(IMessagingConfigurationContext context, NatsReceiveEndpointConfiguration configuration);
}
