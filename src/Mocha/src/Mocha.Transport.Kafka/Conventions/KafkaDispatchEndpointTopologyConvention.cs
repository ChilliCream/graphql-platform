namespace Mocha.Transport.Kafka;

/// <summary>
/// Convention that auto-provisions topics in the topology for dispatch endpoints
/// when they do not already exist.
/// </summary>
public sealed class KafkaDispatchEndpointTopologyConvention : IKafkaDispatchEndpointTopologyConvention
{
    /// <summary>
    /// Discovers and creates missing topology resources (topics) needed by the dispatch endpoint.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="endpoint">The dispatch endpoint being configured.</param>
    /// <param name="configuration">The endpoint configuration specifying the target topic name.</param>
    public void DiscoverTopology(
        IMessagingConfigurationContext context,
        KafkaDispatchEndpoint endpoint,
        KafkaDispatchEndpointConfiguration configuration)
    {
        var topology = (KafkaMessagingTopology)endpoint.Transport.Topology;

        if (configuration.TopicName is not null
            && topology.Topics.FirstOrDefault(t => t.Name == configuration.TopicName) is null)
        {
            topology.AddTopic(new KafkaTopicConfiguration { Name = configuration.TopicName });
        }

        // Ensure error topics exist for dispatch endpoints that route to named topics.
        // Skip endpoints whose topic is already an error or skipped topic to avoid
        // recursive topic creation (e.g. "orders_error_error").
        if (configuration.TopicName is { } topicName
            && !topicName.EndsWith("_error", StringComparison.Ordinal)
            && !topicName.EndsWith("_skipped", StringComparison.Ordinal))
        {
            var errorTopicName = topicName + "_error";
            if (topology.Topics.FirstOrDefault(t => t.Name == errorTopicName) is null)
            {
                topology.AddTopic(new KafkaTopicConfiguration { Name = errorTopicName });
            }
        }
    }
}
