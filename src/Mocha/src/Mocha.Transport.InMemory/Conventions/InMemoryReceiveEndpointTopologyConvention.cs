namespace Mocha.Transport.InMemory;

/// <summary>
/// Default topology convention for in-memory receive endpoints that provisions queues, topics,
/// and bindings based on the endpoint's inbound routes.
/// </summary>
/// <remarks>
/// For each receive endpoint this convention ensures the backing queue exists, creates a
/// same-named topic with a queue binding, and for every routed message type creates the
/// publish/send topic hierarchy with appropriate bindings so that both send and publish
/// patterns deliver messages to the endpoint's queue.
/// </remarks>
public sealed class InMemoryReceiveEndpointTopologyConvention : IInMemoryReceiveEndpointTopologyConvention
{
    /// <summary>
    /// Provisions all topology resources required by the specified receive endpoint and its inbound routes.
    /// </summary>
    /// <param name="context">The messaging configuration context providing naming and routing services.</param>
    /// <param name="endpoint">The receive endpoint being configured.</param>
    /// <param name="configuration">The endpoint configuration containing the target queue name.</param>
    /// <exception cref="InvalidOperationException">The queue name is not set on the configuration.</exception>
    public void DiscoverTopology(
        IMessagingConfigurationContext context,
        InMemoryReceiveEndpoint endpoint,
        InMemoryReceiveEndpointConfiguration configuration)
    {
        if (configuration.QueueName is null)
        {
            throw new InvalidOperationException("Queue name is required");
        }

        var topology = (InMemoryMessagingTopology)endpoint.Transport.Topology;

        if (topology.Queues.FirstOrDefault(q => q.Name == configuration.QueueName) is null)
        {
            topology.AddQueue(new InMemoryQueueConfiguration { Name = configuration.QueueName });
        }

        if (endpoint.Kind is ReceiveEndpointKind.Reply or ReceiveEndpointKind.Error or ReceiveEndpointKind.Skipped)
        {
            return;
        }

        var routes = context.Router.GetInboundByEndpoint(endpoint);
        foreach (var route in routes)
        {
            if (route.MessageType is null)
            {
                continue;
            }

            var publishExchangeName = context.Naming.GetPublishEndpointName(route.MessageType.RuntimeType);
            if (topology.Topics.FirstOrDefault(e => e.Name == publishExchangeName) is null)
            {
                topology.AddTopic(new InMemoryTopicConfiguration { Name = publishExchangeName });
            }

            // make sure the exchange for the message type exists
            var sendExchangeName = context.Naming.GetSendEndpointName(route.MessageType.RuntimeType);
            if (sendExchangeName != publishExchangeName)
            {
                if (topology.Topics.FirstOrDefault(e => e.Name == sendExchangeName) is null)
                {
                    topology.AddTopic(new InMemoryTopicConfiguration { Name = sendExchangeName });
                }

                // make sure the binding between the publish exchange and the send exchange exists
                if (topology.Bindings.FirstOrDefault(b =>
                        b.Source.Name == publishExchangeName
                        && b is InMemoryTopicBinding exchangeBinding
                        && exchangeBinding.Destination.Name == sendExchangeName
                    )
                    is null)
                {
                    topology.AddBinding(
                        new InMemoryBindingConfiguration
                        {
                            Source = publishExchangeName,
                            Destination = sendExchangeName,
                            DestinationKind = InMemoryDestinationKind.Topic
                        });
                }
            }

            // make sure the binding between the exchange and the queue exists
            if (topology.Bindings.FirstOrDefault(b =>
                    b.Source.Name == sendExchangeName
                    && b is InMemoryQueueBinding queueBinding
                    && queueBinding.Destination.Name == configuration.QueueName
                )
                is null)
            {
                topology.AddBinding(
                    new InMemoryBindingConfiguration
                    {
                        Source = sendExchangeName,
                        Destination = configuration.QueueName,
                        DestinationKind = InMemoryDestinationKind.Queue
                    });
            }
        }
    }
}
