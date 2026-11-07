namespace Mocha.Transport.Nats;

/// <summary>
/// Convention interface for applying NATS-specific configuration to receive endpoints.
/// Implementations receive the narrowed <see cref="NatsReceiveEndpointConfiguration"/> type.
/// </summary>
public interface INatsReceiveEndpointConfigurationConvention
    : IEndpointConfigurationConvention<ReceiveEndpointConfiguration>
{
    void IEndpointConfigurationConvention<ReceiveEndpointConfiguration>.Configure(
        IMessagingConfigurationContext context,
        MessagingTransport transport,
        ReceiveEndpointConfiguration configuration)
    {
        if (configuration is not NatsReceiveEndpointConfiguration natsConfiguration)
        {
            return;
        }

        if (transport is not NatsMessagingTransport natsTransport)
        {
            return;
        }

        Configure(context, natsTransport, natsConfiguration);
    }

    /// <summary>
    /// Applies NATS-specific configuration to the given receive endpoint configuration.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="transport">The NATS messaging transport instance.</param>
    /// <param name="configuration">The NATS receive endpoint configuration to modify.</param>
    void Configure(
        IMessagingConfigurationContext context,
        NatsMessagingTransport transport,
        NatsReceiveEndpointConfiguration configuration);
}
