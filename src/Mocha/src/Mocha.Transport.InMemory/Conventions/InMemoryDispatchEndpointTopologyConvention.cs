namespace Mocha.Transport.InMemory;

/// <summary>
/// Default topology convention for in-memory dispatch endpoints that provisions topics and queues
/// referenced by the endpoint configuration.
/// </summary>
public sealed class InMemoryDispatchEndpointTopologyConvention : IInMemoryDispatchEndpointTopologyConvention
{
    /// <summary>
    /// Ensures the topic or queue targeted by the dispatch endpoint exists in the topology.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="endpoint">The dispatch endpoint being configured.</param>
    /// <param name="configuration">The endpoint configuration containing the target topic or queue name.</param>
    public void DiscoverTopology(
        IMessagingConfigurationContext context,
        InMemoryDispatchEndpoint endpoint,
        InMemoryDispatchEndpointConfiguration configuration)
    {
        var topology = (InMemoryMessagingTopology)endpoint.Transport.Topology;

        if (configuration.TopicName is not null
            && topology.Topics.FirstOrDefault(t => t.Name == configuration.TopicName) is null)
        {
            topology.AddTopic(new InMemoryTopicConfiguration { Name = configuration.TopicName });
        }

        if (configuration.QueueName is not null
            && topology.Queues.FirstOrDefault(q => q.Name == configuration.QueueName) is null)
        {
            topology.AddQueue(new InMemoryQueueConfiguration { Name = configuration.QueueName });
        }
    }
}
