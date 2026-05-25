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
    }
}
