namespace Mocha.Transport.Kafka;

/// <summary>
/// Default convention that assigns topic names, error endpoints, and skipped endpoints
/// to Kafka receive endpoint configurations that do not already have them set.
/// </summary>
public sealed class KafkaDefaultReceiveEndpointConvention : IKafkaReceiveEndpointConfigurationConvention
{
    /// <inheritdoc />
    public void Configure(
        IMessagingConfigurationContext context,
        KafkaMessagingTransport transport,
        KafkaReceiveEndpointConfiguration configuration)
    {
        configuration.TopicName ??= configuration.Name;
        configuration.ConsumerGroupId ??= configuration.Name;

        if (configuration is { Kind: ReceiveEndpointKind.Default, TopicName: { } topicName })
        {
            if (configuration.ErrorEndpoint is null)
            {
                var errorName = context.Naming.GetReceiveEndpointName(topicName, ReceiveEndpointKind.Error);
                configuration.ErrorEndpoint = new Uri($"{transport.Schema}:///t/{errorName}");
            }

            if (configuration.SkippedEndpoint is null)
            {
                var skippedName = context.Naming.GetReceiveEndpointName(topicName, ReceiveEndpointKind.Skipped);
                configuration.SkippedEndpoint = new Uri($"{transport.Schema}:///t/{skippedName}");
            }
        }
    }
}
