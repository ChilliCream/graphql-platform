namespace Mocha.Transport.InMemory;

/// <summary>
/// Convention that applies default configuration values to in-memory receive endpoint configurations.
/// </summary>
/// <remarks>
/// Implementations receive the strongly-typed <see cref="InMemoryReceiveEndpointConfiguration"/>
/// instead of the base <see cref="ReceiveEndpointConfiguration"/>, enabling transport-specific defaults.
/// </remarks>
public interface IInMemoryReceiveEndpointConfigurationConvention
    : IEndpointConfigurationConvention<ReceiveEndpointConfiguration>
{
    void IEndpointConfigurationConvention<ReceiveEndpointConfiguration>.Configure(
        IMessagingConfigurationContext context,
        MessagingTransport transport,
        ReceiveEndpointConfiguration configuration)
    {
        if (configuration is not InMemoryReceiveEndpointConfiguration inMemoryConfiguration)
        {
            return;
        }

        if (transport is not InMemoryMessagingTransport inMemoryTransport)
        {
            return;
        }

        Configure(context, inMemoryTransport, inMemoryConfiguration);
    }

    /// <summary>
    /// Applies convention-defined defaults to an in-memory receive endpoint configuration.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="transport">The in-memory messaging transport instance.</param>
    /// <param name="configuration">The strongly-typed in-memory receive endpoint configuration to modify.</param>
    void Configure(
        IMessagingConfigurationContext context,
        InMemoryMessagingTransport transport,
        InMemoryReceiveEndpointConfiguration configuration);
}
