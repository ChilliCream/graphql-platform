namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Convention that ensures topics exist in the topology for receive endpoints
/// creating topic entries for the endpoint's hub name and for each inbound message route.
/// </summary>
public sealed class EventHubReceiveEndpointTopologyConvention : IEventHubReceiveEndpointTopologyConvention
{
    /// <inheritdoc />
    public void DiscoverTopology(
        IMessagingConfigurationContext context,
        EventHubReceiveEndpoint endpoint,
        EventHubReceiveEndpointConfiguration configuration)
    {
        if (configuration.HubName is null)
        {
            throw new InvalidOperationException("Hub name is required");
        }

        var topology = (EventHubMessagingTopology)endpoint.Transport.Topology;

        // Ensure topic exists in topology model
        if (topology.Topics.FirstOrDefault(t => t.Name == configuration.HubName) is null)
        {
            topology.AddTopic(new EventHubTopicConfiguration
            {
                Name = configuration.HubName,
                AutoProvision = configuration.AutoProvision
            });
        }

        // Ensure consumer group exists in topology model (skip $Default — it always exists)
        var consumerGroup = configuration.ConsumerGroup ?? "$Default";
        if (consumerGroup != "$Default"
            && topology.Subscriptions.FirstOrDefault(
                s => s.TopicName == configuration.HubName && s.ConsumerGroup == consumerGroup) is null)
        {
            topology.AddSubscription(new EventHubSubscriptionConfiguration
            {
                TopicName = configuration.HubName,
                ConsumerGroup = consumerGroup,
                AutoProvision = configuration.AutoProvision
            });
        }

        if (endpoint.Kind is ReceiveEndpointKind.Reply or ReceiveEndpointKind.Error
            or ReceiveEndpointKind.Skipped)
        {
            return;
        }

        // For each inbound route, ensure a topic exists for that message type
        var routes = context.Router.GetInboundByEndpoint(endpoint);
        foreach (var route in routes)
        {
            if (route.MessageType is null)
            {
                continue;
            }

            var publishHubName = context.Naming.GetPublishEndpointName(route.MessageType.RuntimeType);
            if (topology.Topics.FirstOrDefault(t => t.Name == publishHubName) is null)
            {
                topology.AddTopic(new EventHubTopicConfiguration { Name = publishHubName });
            }

            var sendHubName = context.Naming.GetSendEndpointName(route.MessageType.RuntimeType);
            if (sendHubName != publishHubName
                && topology.Topics.FirstOrDefault(t => t.Name == sendHubName) is null)
            {
                topology.AddTopic(new EventHubTopicConfiguration { Name = sendHubName });
            }
        }
    }
}
