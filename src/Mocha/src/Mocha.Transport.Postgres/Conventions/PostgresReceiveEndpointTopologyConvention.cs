namespace Mocha.Transport.Postgres;

/// <summary>
/// Default topology convention for PostgreSQL receive endpoints that provisions topics and
/// subscriptions based on the endpoint's inbound routes. Auto-binding is resolved per route
/// with the type, queue, transport precedence; when it is off, the subscription into this queue
/// is suppressed while the type-owned publish and send topics are still produced.
/// </summary>
public sealed class PostgresReceiveEndpointTopologyConvention : IPostgresReceiveEndpointTopologyConvention
{
    /// <summary>
    /// Discovers and creates the missing topics and subscriptions needed by the receive endpoint based
    /// on its inbound message routes, subscribing them to the endpoint's existing queue. Auto-binding
    /// is resolved per route with the type, queue, transport precedence (3.4): when it is off, only
    /// the convention subscription into this queue is suppressed; the type-owned publish and send topics
    /// remain so a second endpoint that does auto-bind the same type keeps a complete chain.
    /// </summary>
    /// <param name="context">The messaging configuration context providing naming and routing services.</param>
    /// <param name="endpoint">The receive endpoint being configured.</param>
    /// <param name="configuration">The endpoint configuration containing the target queue name.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the queue name is not set on the configuration, or if a consumed type names an
    /// explicit queue destination that has no topic chain to subscribe from.
    /// </exception>
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

        if (endpoint.Kind is ReceiveEndpointKind.Reply or ReceiveEndpointKind.Error or ReceiveEndpointKind.Skipped)
        {
            return;
        }

        var resolver = ((PostgresMessagingTransport)endpoint.Transport).Resolver;

        var routes = context.Router.GetInboundByEndpoint(endpoint);
        foreach (var route in routes)
        {
            // Reply routes are address-routed and must never produce a convention chain.
            if (route.Kind is InboundRouteKind.Reply)
            {
                continue;
            }

            if (route.MessageType is null)
            {
                continue;
            }

            // Auto-binding is resolved per route with the type > queue > transport precedence (3.4).
            // When it is off, the only effect is that no convention subscription into this queue is
            // generated for the type; the type-owned publish/send topics are still built so a second
            // endpoint that does auto-bind the same type keeps a complete chain (suppression scope).
            var autoBind = ResolveAutoBind(endpoint.Transport, configuration, route.MessageType);

            var chainEntry = ResolveChainEntry(context, resolver, route.MessageType);

            // An explicit queue destination on a consumed type has no topic chain to subscribe from,
            // so the subscription is underivable. Fail the build instead of guessing when auto-binding
            // is on; with auto-binding off, no subscription is derived so no error is emitted.
            if (chainEntry.Kind == PostgresDestinationKind.Queue)
            {
                if (autoBind)
                {
                    throw ThrowHelper.ConsumeBindUnderivable(GetTypeName(route.MessageType), configuration.QueueName);
                }

                continue;
            }

            // An explicitly named topic is subscribed directly into the endpoint queue; the convention
            // publish/send sublayer applies only to convention-named destinations.
            if (chainEntry.IsExplicit)
            {
                EnsureTopic(topology, chainEntry.Name);

                if (autoBind)
                {
                    EnsureSubscription(topology, chainEntry.Name, configuration.QueueName);
                }

                continue;
            }

            var publishTopicName = chainEntry.Name;
            EnsureTopic(topology, publishTopicName);

            // make sure the topic for the message type exists
            var sendTopicName = context.Naming.GetSendEndpointName(route.MessageType.RuntimeType);
            if (sendTopicName != publishTopicName)
            {
                EnsureTopic(topology, sendTopicName);

                // make sure the subscription between the publish topic and the endpoint queue exists;
                // the subscription is the only artifact auto-binding gates here.
                if (autoBind)
                {
                    EnsureSubscription(topology, publishTopicName, configuration.QueueName);
                }
            }

            // make sure the subscription between the send topic and the queue exists
            if (autoBind)
            {
                EnsureSubscription(topology, sendTopicName, configuration.QueueName);
            }
        }
    }

    private static bool ResolveAutoBind(
        MessagingTransport transport,
        PostgresReceiveEndpointConfiguration configuration,
        MessageType messageType)
    {
        // Type scope wins over queue scope, which wins over transport scope; default on.
        if (configuration.TypeBinds.TryGetValue(messageType.RuntimeType, out var typeBind)
            && typeBind.AutoBind.HasValue)
        {
            return typeBind.AutoBind.Value;
        }

        if (configuration.AutoBind.HasValue)
        {
            return configuration.AutoBind.Value;
        }

        return transport.AutoBind;
    }

    private static ChainEntry ResolveChainEntry(
        IMessagingConfigurationContext context,
        PostgresDestinationResolver resolver,
        MessageType messageType)
    {
        foreach (var route in context.Router.GetOutboundByMessageType(messageType))
        {
            if (route is { HasExplicitDestination: true, Kind: OutboundRouteKind.Publish })
            {
                var resolution = resolver.ResolveDestination(context.Naming, route);
                return new ChainEntry(resolution.Kind, resolution.Name, IsExplicit: true);
            }
        }

        var conventionTopic = resolver.ResolvePublishDestination(context.Naming, messageType);
        return new ChainEntry(conventionTopic.Kind, conventionTopic.Name, IsExplicit: false);
    }

    private static void EnsureTopic(PostgresMessagingTopology topology, string topicName)
    {
        if (topology.Topics.FirstOrDefault(e => e.Name == topicName) is null)
        {
            topology.AddTopic(new PostgresTopicConfiguration { Name = topicName });
        }
    }

    private static void EnsureSubscription(
        PostgresMessagingTopology topology,
        string sourceTopicName,
        string queueName)
    {
        if (topology.Subscriptions.FirstOrDefault(s =>
                s.Source.Name == sourceTopicName && s.Destination.Name == queueName) is null)
        {
            topology.AddSubscription(
                new PostgresSubscriptionConfiguration
                {
                    Source = sourceTopicName,
                    Destination = queueName
                });
        }
    }

    private static string GetTypeName(MessageType messageType)
        => messageType.RuntimeType.FullName ?? messageType.RuntimeType.Name;

    private readonly record struct ChainEntry(PostgresDestinationKind Kind, string Name, bool IsExplicit);
}
