namespace Mocha.Transport.RabbitMQ;

// TODO i mean technically we could make the error and the skipped queue a simple extension
// for this we JUST need a endpoint interceptor and a middleware
/// <summary>
/// Default convention that assigns queue names, error endpoints, and skipped endpoints
/// to RabbitMQ receive endpoint configurations that do not already have them set.
/// </summary>
public sealed class RabbitMQDefaultReceiveEndpointConvention : IRabbitMQReceiveEndpointConfigurationConvention
{
    /// <inheritdoc />
    public void Configure(
        IMessagingConfigurationContext context,
        RabbitMQMessagingTransport transport,
        RabbitMQReceiveEndpointConfiguration configuration)
    {
        configuration.QueueName ??= configuration.Name;

        if (configuration is { Kind: ReceiveEndpointKind.Default, QueueName: { } queueName })
        {
            // The error and skip queues serve the endpoint's queue, so they inherit that queue's
            // auto-provision value. The declared queue carries the explicit value when present, and
            // the endpoint configuration is used as a fallback for convention-created queues. A
            // satellite may override the inherited value independently.
            var declaredQueue = transport.Configuration is RabbitMQTransportConfiguration rabbitMQConfiguration
                ? rabbitMQConfiguration.Queues.FirstOrDefault(q => q.Name == queueName)
                : null;
            var inheritedAutoProvision = declaredQueue?.AutoProvision ?? configuration.AutoProvision;

            MaterializeSatellite(
                context,
                transport,
                configuration.ErrorQueue,
                queueName,
                ReceiveEndpointKind.Error,
                inheritedAutoProvision,
                endpoint => configuration.ErrorEndpoint ??= endpoint);

            MaterializeSatellite(
                context,
                transport,
                configuration.SkippedQueue,
                queueName,
                ReceiveEndpointKind.Skipped,
                inheritedAutoProvision,
                endpoint => configuration.SkippedEndpoint ??= endpoint);
        }
    }

    private static void MaterializeSatellite(
        IMessagingConfigurationContext context,
        RabbitMQMessagingTransport transport,
        RabbitMQSatelliteConfiguration satellite,
        string queueName,
        ReceiveEndpointKind kind,
        bool? inheritedAutoProvision,
        Action<Uri> assign)
    {
        if (satellite.IsDisabled)
        {
            return;
        }

        // A satellite name is stored verbatim when provided and bypasses the naming convention,
        // so literal names such as "LEGACY.Orders.Error" survive unchanged. When no name is set,
        // the convention-derived name keeps the historical default.
        var name = satellite.QueueName ?? context.Naming.GetReceiveEndpointName(queueName, kind);

        satellite.AutoProvision ??= inheritedAutoProvision;

        assign(new Uri($"{transport.Schema}:q/{name}"));
    }
}
