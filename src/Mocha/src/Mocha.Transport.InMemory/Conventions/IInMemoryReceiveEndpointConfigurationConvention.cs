namespace Mocha.Transport.InMemory;

/// <summary>
/// Convention that applies default configuration values to in-memory receive endpoint configurations.
/// </summary>
/// <remarks>
/// Implementations receive the strongly-typed <see cref="InMemoryReceiveEndpointConfiguration"/>
/// instead of the base <see cref="ReceiveEndpointConfiguration"/>, enabling transport-specific defaults.
/// </remarks>
public interface IInMemoryReceiveEndpointConfigurationConvention : IReceiveEndpointConvention
{
    void IConfigurationConvention<ReceiveEndpointConfiguration>.Configure(
        IMessagingConfigurationContext context,
        ReceiveEndpointConfiguration configuration)
    {
        if (configuration is not InMemoryReceiveEndpointConfiguration inMemoryConfiguration)
        {
            return;
        }

        Configure(context, inMemoryConfiguration);
    }

    /// <summary>
    /// Applies convention-defined defaults to an in-memory receive endpoint configuration.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="configuration">The strongly-typed in-memory receive endpoint configuration to modify.</param>
    void Configure(IMessagingConfigurationContext context, InMemoryReceiveEndpointConfiguration configuration);
}
