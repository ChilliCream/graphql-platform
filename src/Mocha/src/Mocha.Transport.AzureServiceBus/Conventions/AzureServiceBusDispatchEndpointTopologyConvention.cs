namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Default topology convention for Azure Service Bus dispatch endpoints that provisions topics and queues
/// referenced by the endpoint configuration.
/// </summary>
public sealed class AzureServiceBusDispatchEndpointTopologyConvention
    : IAzureServiceBusDispatchEndpointTopologyConvention
{
    /// <summary>
    /// Ensures the topic or queue targeted by the dispatch endpoint exists in the topology.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="endpoint">The dispatch endpoint being configured.</param>
    /// <param name="configuration">The endpoint configuration containing the target topic or queue name.</param>
    public void DiscoverTopology(
        IMessagingConfigurationContext context,
        AzureServiceBusDispatchEndpoint endpoint,
        AzureServiceBusDispatchEndpointConfiguration configuration)
    {
        var topology = (AzureServiceBusMessagingTopology)endpoint.Transport.Topology;

        if (configuration.TopicName is not null
            && topology.Topics.FirstOrDefault(t => t.Name == configuration.TopicName) is null)
        {
            topology.AddTopic(new AzureServiceBusTopicConfiguration { Name = configuration.TopicName });
        }

        if (configuration.QueueName is not null
            && topology.Queues.FirstOrDefault(q => q.Name == configuration.QueueName) is null)
        {
            topology.AddQueue(new AzureServiceBusQueueConfiguration { Name = configuration.QueueName });
        }

        // Provision convention topics for each route so that routing is consistent
        // across sender/receiver boundaries.
        if (configuration.TopicName is not null)
        {
            foreach (var (runtimeType, kind) in configuration.Routes)
            {
                var conventionTopicName =
                    kind == OutboundRouteKind.Publish
                        ? context.Naming.GetPublishEndpointName(runtimeType)
                        : context.Naming.GetSendEndpointName(runtimeType);

                if (configuration.TopicName == conventionTopicName)
                {
                    continue;
                }

                if (topology.Topics.FirstOrDefault(t => t.Name == conventionTopicName) is null)
                {
                    topology.AddTopic(new AzureServiceBusTopicConfiguration { Name = conventionTopicName });
                }
            }
        }
    }
}
