namespace Mocha.Transport.Postgres;

/// <summary>
/// Default topology convention for PostgreSQL dispatch endpoints that provisions topics and queues
/// referenced by the endpoint configuration. In implicit mode, bridge topics are created so the
/// producer and consumer paths converge on the same entity via the destination resolver.
/// </summary>
public sealed class PostgresDispatchEndpointTopologyConvention : IPostgresDispatchEndpointTopologyConvention
{
    /// <summary>
    /// Ensures the topic or queue targeted by the dispatch endpoint exists in the topology. In
    /// implicit mode, creates bridge topics for each route so the producer and consumer conventions
    /// converge on the same entity.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="endpoint">The dispatch endpoint being configured.</param>
    /// <param name="configuration">The endpoint configuration containing the target topic or queue name.</param>
    public void DiscoverTopology(
        IMessagingConfigurationContext context,
        PostgresDispatchEndpoint endpoint,
        PostgresDispatchEndpointConfiguration configuration)
    {
        var topology = (PostgresMessagingTopology)endpoint.Transport.Topology;

        if (configuration.TopicName is not null
            && topology.Topics.FirstOrDefault(t => t.Name == configuration.TopicName) is null)
        {
            topology.AddTopic(new PostgresTopicConfiguration { Name = configuration.TopicName });
        }

        if (configuration.QueueName is not null
            && topology.Queues.FirstOrDefault(q => q.Name == configuration.QueueName) is null)
        {
            topology.AddQueue(new PostgresQueueConfiguration { Name = configuration.QueueName });
        }

        // In implicit mode, ensure convention topics for each route exist so the producer and
        // consumer conventions converge on the same entity.
        if (configuration.TopicName is not null
            && endpoint.Transport.BindMode == MessagingBindMode.Implicit)
        {
            var resolver = ((PostgresMessagingTransport)endpoint.Transport).Resolver;

            foreach (var (runtimeType, kind) in configuration.Routes)
            {
                var messageType = context.Messages.GetMessageType(runtimeType);
                if (messageType is null)
                {
                    continue;
                }

                // Find the full outbound route to let the resolver honor any explicit destination.
                var outboundRoute = context.Router.GetOutboundByMessageType(messageType)
                    .FirstOrDefault(r => r.Kind == kind);

                PostgresDestinationResolution chainEntry;
                if (outboundRoute is not null)
                {
                    chainEntry = resolver.ResolveDestination(context.Naming, outboundRoute);
                }
                else
                {
                    // Route not yet registered: fall back to the convention topic name.
                    chainEntry = kind == OutboundRouteKind.Publish
                        ? resolver.ResolvePublishDestination(context.Naming, messageType)
                        : new PostgresDestinationResolution(
                            PostgresDestinationKind.Topic,
                            context.Naming.GetSendEndpointName(runtimeType),
                            "t/" + context.Naming.GetSendEndpointName(runtimeType));
                }

                // An explicit queue destination has no topic chain to bridge from.
                if (chainEntry.Kind == PostgresDestinationKind.Queue)
                {
                    continue;
                }

                var chainTopicName = chainEntry.Name;

                if (configuration.TopicName == chainTopicName)
                {
                    continue;
                }

                if (topology.Topics.FirstOrDefault(t => t.Name == chainTopicName) is null)
                {
                    topology.AddTopic(new PostgresTopicConfiguration { Name = chainTopicName });
                }
            }
        }
    }
}
