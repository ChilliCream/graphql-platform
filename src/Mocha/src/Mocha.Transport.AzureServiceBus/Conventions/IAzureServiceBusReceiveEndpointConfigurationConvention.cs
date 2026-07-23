namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Convention interface for applying Azure Service Bus-specific configuration to receive endpoints.
/// Implementations receive the narrowed <see cref="AzureServiceBusReceiveEndpointConfiguration"/> type.
/// </summary>
public interface IAzureServiceBusReceiveEndpointConfigurationConvention
    : IEndpointConfigurationConvention<ReceiveEndpointConfiguration>
{
    void IEndpointConfigurationConvention<ReceiveEndpointConfiguration>.Configure(
        IMessagingConfigurationContext context,
        MessagingTransport transport,
        ReceiveEndpointConfiguration configuration)
    {
        if (configuration is not AzureServiceBusReceiveEndpointConfiguration azureServiceBusConfiguration)
        {
            return;
        }

        if (transport is not AzureServiceBusMessagingTransport azureServiceBusTransport)
        {
            return;
        }

        Configure(context, azureServiceBusTransport, azureServiceBusConfiguration);
    }

    /// <summary>
    /// Applies Azure Service Bus-specific configuration to the given receive endpoint configuration.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="transport">The Azure Service Bus messaging transport instance.</param>
    /// <param name="configuration">The Azure Service Bus receive endpoint configuration to modify.</param>
    void Configure(
        IMessagingConfigurationContext context,
        AzureServiceBusMessagingTransport transport,
        AzureServiceBusReceiveEndpointConfiguration configuration);
}
