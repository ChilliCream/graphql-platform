namespace Mocha.Transport.Kafka;

/// <summary>
/// Convention that auto-provisions topics in the topology for receive endpoints,
/// creating the necessary topics for each inbound route, error topics, and reply topics
/// with appropriate retention settings.
/// </summary>
public sealed class KafkaReceiveEndpointTopologyConvention : IKafkaReceiveEndpointTopologyConvention
{
    /// <summary>
    /// Discovers and creates missing topology resources (topics) needed by the receive endpoint
    /// based on its inbound message routes.
    /// </summary>
    /// <param name="context">The messaging configuration context providing naming and routing information.</param>
    /// <param name="endpoint">The receive endpoint being configured.</param>
    /// <param name="configuration">The endpoint configuration specifying the source topic name.</param>
    /// <exception cref="InvalidOperationException">Thrown if the topic name is not set on the configuration.</exception>
    public void DiscoverTopology(
        IMessagingConfigurationContext context,
        KafkaReceiveEndpoint endpoint,
        KafkaReceiveEndpointConfiguration configuration)
    {
        if (configuration.TopicName is null)
        {
            throw new InvalidOperationException("Topic name is required");
        }

        var topology = (KafkaMessagingTopology)endpoint.Transport.Topology;

        // Ensure the main topic exists.
        if (topology.Topics.FirstOrDefault(t => t.Name == configuration.TopicName) is null)
        {
            var topicConfig = new KafkaTopicConfiguration
            {
                Name = configuration.TopicName,
                IsTemporary = endpoint.Kind == ReceiveEndpointKind.Reply,
                AutoProvision = configuration.AutoProvision
            };

            // Reply topics get short retention for self-cleanup.
            if (endpoint.Kind == ReceiveEndpointKind.Reply)
            {
                topicConfig.TopicConfigs = new Dictionary<string, string>
                {
                    ["retention.ms"] = "3600000",
                    ["cleanup.policy"] = "delete"
                };
            }

            topology.AddTopic(topicConfig);
        }

        if (endpoint.Kind is ReceiveEndpointKind.Reply or ReceiveEndpointKind.Error or ReceiveEndpointKind.Skipped)
        {
            return;
        }

        // Ensure error topic exists for default endpoints.
        var errorTopicName = configuration.TopicName + "_error";
        if (topology.Topics.FirstOrDefault(t => t.Name == errorTopicName) is null)
        {
            topology.AddTopic(new KafkaTopicConfiguration { Name = errorTopicName });
        }

        // Ensure topics exist for each inbound route's message type.
        var routes = context.Router.GetInboundByEndpoint(endpoint);
        foreach (var route in routes)
        {
            if (route.MessageType is null)
            {
                continue;
            }

            var publishTopicName = context.Naming.GetPublishEndpointName(route.MessageType.RuntimeType);
            if (publishTopicName != configuration.TopicName
                && topology.Topics.FirstOrDefault(t => t.Name == publishTopicName) is null)
            {
                topology.AddTopic(new KafkaTopicConfiguration { Name = publishTopicName });
            }

            var sendTopicName = context.Naming.GetSendEndpointName(route.MessageType.RuntimeType);
            if (sendTopicName != configuration.TopicName
                && sendTopicName != publishTopicName
                && topology.Topics.FirstOrDefault(t => t.Name == sendTopicName) is null)
            {
                topology.AddTopic(new KafkaTopicConfiguration { Name = sendTopicName });
            }
        }
    }
}
