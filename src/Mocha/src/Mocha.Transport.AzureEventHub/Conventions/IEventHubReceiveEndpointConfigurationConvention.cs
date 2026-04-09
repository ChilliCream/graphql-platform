namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Convention interface for applying Event Hub-specific configuration to receive endpoints.
/// Implementations receive the narrowed <see cref="EventHubReceiveEndpointConfiguration"/> type.
/// </summary>
public interface IEventHubReceiveEndpointConfigurationConvention
    : IEndpointConfigurationConvention<ReceiveEndpointConfiguration>
{
    void IEndpointConfigurationConvention<ReceiveEndpointConfiguration>.Configure(
        IMessagingConfigurationContext context,
        MessagingTransport transport,
        ReceiveEndpointConfiguration configuration)
    {
        if (configuration is not EventHubReceiveEndpointConfiguration eventHubConfiguration)
        {
            return;
        }

        if (transport is not EventHubMessagingTransport eventHubTransport)
        {
            return;
        }

        Configure(context, eventHubTransport, eventHubConfiguration);
    }

    /// <summary>
    /// Applies Event Hub-specific configuration to the given receive endpoint configuration.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="transport">The Event Hub messaging transport instance.</param>
    /// <param name="configuration">The Event Hub receive endpoint configuration to modify.</param>
    void Configure(
        IMessagingConfigurationContext context,
        EventHubMessagingTransport transport,
        EventHubReceiveEndpointConfiguration configuration);
}
