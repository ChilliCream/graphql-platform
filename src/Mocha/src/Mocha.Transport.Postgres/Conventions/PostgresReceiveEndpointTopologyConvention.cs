namespace Mocha.Transport.Postgres;

/// <summary>
/// Default topology convention for PostgreSQL receive endpoints that provisions queues, topics,
/// and subscriptions based on the endpoint's inbound routes.
/// </summary>
/// <remarks>
/// For each receive endpoint this convention ensures the backing queue exists, and for every
/// routed message type creates the publish/send topic hierarchy with appropriate subscriptions
/// so that both send and publish patterns deliver messages to the endpoint's queue.
/// </remarks>
public sealed class PostgresReceiveEndpointTopologyConvention : IPostgresReceiveEndpointTopologyConvention
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
        PostgresReceiveEndpoint endpoint,
        PostgresReceiveEndpointConfiguration configuration)
    {
        if (configuration.QueueName is null)
        {
            throw new InvalidOperationException("Queue name is required");
        }

        var topology = (PostgresMessagingTopology)endpoint.Transport.Topology;

        if (topology.Queues.FirstOrDefault(q => q.Name == configuration.QueueName) is null)
        {
            topology.AddQueue(
                new PostgresQueueConfiguration
                {
                    Name = configuration.QueueName,
                    AutoDelete = endpoint.Kind == ReceiveEndpointKind.Reply,
                    AutoProvision = configuration.AutoProvision
                });
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

            var publishTopicName = context.Naming.GetPublishEndpointName(route.MessageType.RuntimeType);
            if (topology.Topics.FirstOrDefault(e => e.Name == publishTopicName) is null)
            {
                topology.AddTopic(new PostgresTopicConfiguration { Name = publishTopicName });
            }

            // make sure the topic for the message type exists
            var sendTopicName = context.Naming.GetSendEndpointName(route.MessageType.RuntimeType);
            if (sendTopicName != publishTopicName)
            {
                if (topology.Topics.FirstOrDefault(e => e.Name == sendTopicName) is null)
                {
                    topology.AddTopic(new PostgresTopicConfiguration { Name = sendTopicName });
                }

                // make sure the subscription between the publish topic and the send topic's queue exists
                if (topology.Subscriptions.FirstOrDefault(s =>
                        s.Source.Name == publishTopicName && s.Destination.Name == configuration.QueueName) is null)
                {
                    topology.AddSubscription(
                        new PostgresSubscriptionConfiguration
                        {
                            Source = publishTopicName,
                            Destination = configuration.QueueName
                        });
                }
            }

            // make sure the subscription between the send topic and the queue exists
            if (topology.Subscriptions.FirstOrDefault(s =>
                    s.Source.Name == sendTopicName && s.Destination.Name == configuration.QueueName) is null)
            {
                topology.AddSubscription(
                    new PostgresSubscriptionConfiguration
                    {
                        Source = sendTopicName,
                        Destination = configuration.QueueName
                    });
            }
        }
    }
}
