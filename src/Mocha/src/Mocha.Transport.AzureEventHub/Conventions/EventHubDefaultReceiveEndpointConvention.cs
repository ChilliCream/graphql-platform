namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Default convention that assigns hub names, error endpoints, and skipped endpoints
/// to Event Hub receive endpoint configurations that do not already have them set.
/// Uses shared error/skipped hubs per transport to minimize the number of required Event Hub entities.
/// </summary>
public sealed class EventHubDefaultReceiveEndpointConvention : IEventHubReceiveEndpointConfigurationConvention
{
    /// <inheritdoc />
    public void Configure(
        IMessagingConfigurationContext context,
        EventHubMessagingTransport transport,
        EventHubReceiveEndpointConfiguration configuration)
    {
        configuration.HubName ??= configuration.Name;
        configuration.ConsumerGroup ??= "$Default";

        if (configuration is { Kind: ReceiveEndpointKind.Default })
        {
            if (configuration.ErrorEndpoint is null)
            {
                // Shared error hub for the entire transport
                configuration.ErrorEndpoint = new Uri($"{transport.Schema}:h/error");
            }

            if (configuration.SkippedEndpoint is null)
            {
                // Shared skipped hub for the entire transport
                configuration.SkippedEndpoint = new Uri($"{transport.Schema}:h/skipped");
            }
        }
    }
}
