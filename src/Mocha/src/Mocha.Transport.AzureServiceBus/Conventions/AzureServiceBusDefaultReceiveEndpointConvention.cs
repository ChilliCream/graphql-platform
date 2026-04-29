namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Default convention that assigns queue names, error endpoints, and skipped endpoints
/// to Azure Service Bus receive endpoint configurations that do not already have them set,
/// and applies bus-level endpoint defaults.
/// </summary>
public sealed class AzureServiceBusDefaultReceiveEndpointConvention
    : IAzureServiceBusReceiveEndpointConfigurationConvention
{
    /// <inheritdoc />
    public void Configure(
        IMessagingConfigurationContext context,
        AzureServiceBusMessagingTransport transport,
        AzureServiceBusReceiveEndpointConfiguration configuration)
    {
        configuration.QueueName ??= configuration.Name;

        if (configuration is { Kind: ReceiveEndpointKind.Default, QueueName: { } queueName })
        {
            if (configuration.ErrorEndpoint is null)
            {
                var errorName = context.Naming.GetReceiveEndpointName(queueName, ReceiveEndpointKind.Error);
                configuration.ErrorEndpoint = new Uri($"{transport.Schema}:q/{errorName}");
            }

            if (configuration.SkippedEndpoint is null)
            {
                var skippedName = context.Naming.GetReceiveEndpointName(queueName, ReceiveEndpointKind.Skipped);
                configuration.SkippedEndpoint = new Uri($"{transport.Schema}:q/{skippedName}");
            }
        }

        if (transport.Topology is AzureServiceBusMessagingTopology topology)
        {
            topology.Defaults.Endpoint.ApplyTo(configuration);
        }
    }
}
