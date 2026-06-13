namespace Mocha.Transport.InMemory;

/// <summary>
/// Default topology convention for in-memory receive endpoints that provisions topics and
/// bindings based on the endpoint's inbound routes. Auto-binding is resolved per route with the
/// type, queue, transport precedence; when it is off, the binding into this queue is suppressed
/// while the type-owned publish and send topics are still produced.
/// </summary>
/// <remarks>
/// For every routed message type this convention creates the publish/send topic hierarchy with
/// appropriate bindings so that both send and publish patterns deliver messages to the
/// endpoint's queue. The endpoint owns its queue, so this convention binds to it but never
/// creates it. The chain entry is resolved through the transport's destination resolver so the
/// producer and consumer paths converge on the same entity.
/// </remarks>
public sealed class InMemoryReceiveEndpointTopologyConvention : IInMemoryReceiveEndpointTopologyConvention
{
    /// <summary>
    /// Provisions all topology resources required by the specified receive endpoint and its inbound routes.
    /// Auto-binding is resolved per route with the type, queue, transport precedence (3.4): when it is off,
    /// only the convention binding into this queue is suppressed; the type-owned publish and send topics
    /// remain so a second endpoint that does auto-bind the same type keeps a complete chain.
    /// </summary>
    /// <param name="context">The messaging configuration context providing naming and routing services.</param>
    /// <param name="endpoint">The receive endpoint being configured.</param>
    /// <param name="configuration">The endpoint configuration containing the target queue name.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the queue name is not set on the configuration, or if a consumed type names an
    /// explicit queue destination that has no topic chain to bind from.
    /// </exception>
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

        if (endpoint.Kind is ReceiveEndpointKind.Reply or ReceiveEndpointKind.Error or ReceiveEndpointKind.Skipped)
        {
            return;
        }

        var resolver = ((InMemoryMessagingTransport)endpoint.Transport).Resolver;

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
            // When it is off, the only effect is that no convention binding into this queue is
            // generated for the type; the type-owned publish/send topics are still built so a second
            // endpoint that does auto-bind the same type keeps a complete chain (suppression scope).
            var autoBind = ResolveAutoBind(endpoint.Transport, configuration, route.MessageType);

            var chainEntry = ResolveChainEntry(context, resolver, route.MessageType);

            // An explicit queue destination on a consumed type has no topic chain to bind from, so the
            // binding is underivable. Fail the build instead of guessing when auto-binding is on; with
            // auto-binding off, no binding is derived so no error is emitted.
            if (chainEntry.Kind == InMemoryDestinationKind.Queue)
            {
                if (autoBind)
                {
                    throw ThrowHelper.ConsumeBindUnderivable(GetTypeName(route.MessageType), configuration.QueueName);
                }

                continue;
            }

            // An explicitly named topic is bound directly into the endpoint queue; the convention
            // publish/send sublayer applies only to convention-named destinations.
            if (chainEntry.IsExplicit)
            {
                EnsureTopic(topology, chainEntry.Name);

                if (autoBind)
                {
                    EnsureQueueBinding(topology, chainEntry.Name, configuration.QueueName);
                }

                continue;
            }

            var publishTopicName = chainEntry.Name;
            EnsureTopic(topology, publishTopicName);

            var sendTopicName = context.Naming.GetSendEndpointName(route.MessageType.RuntimeType);
            if (sendTopicName != publishTopicName)
            {
                EnsureTopic(topology, sendTopicName);

                // bind the publish topic to the send topic
                EnsureTopicBinding(topology, publishTopicName, sendTopicName);
            }

            // The bind into this queue is the only artifact auto-binding gates; the topic chain above
            // is type-owned and remains so a second endpoint binding the same type stays complete.
            if (autoBind)
            {
                EnsureQueueBinding(topology, sendTopicName, configuration.QueueName);
            }
        }
    }

    private static bool ResolveAutoBind(
        MessagingTransport transport,
        InMemoryReceiveEndpointConfiguration configuration,
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
        InMemoryDestinationResolver resolver,
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

    private static void EnsureTopic(InMemoryMessagingTopology topology, string topicName)
    {
        if (topology.Topics.FirstOrDefault(e => e.Name == topicName) is null)
        {
            topology.AddTopic(new InMemoryTopicConfiguration { Name = topicName });
        }
    }

    private static void EnsureTopicBinding(
        InMemoryMessagingTopology topology,
        string sourceTopicName,
        string destinationTopicName)
    {
        if (topology.Bindings.FirstOrDefault(b =>
                b.Source.Name == sourceTopicName
                && b is InMemoryTopicBinding topicBinding
                && topicBinding.Destination.Name == destinationTopicName) is null)
        {
            topology.AddBinding(
                new InMemoryBindingConfiguration
                {
                    Source = sourceTopicName,
                    Destination = destinationTopicName,
                    DestinationKind = InMemoryDestinationKind.Topic
                });
        }
    }

    private static void EnsureQueueBinding(
        InMemoryMessagingTopology topology,
        string sourceTopicName,
        string queueName)
    {
        if (topology.Bindings.FirstOrDefault(b =>
                b.Source.Name == sourceTopicName
                && b is InMemoryQueueBinding queueBinding
                && queueBinding.Destination.Name == queueName) is null)
        {
            topology.AddBinding(
                new InMemoryBindingConfiguration
                {
                    Source = sourceTopicName,
                    Destination = queueName,
                    DestinationKind = InMemoryDestinationKind.Queue
                });
        }
    }

    private static string GetTypeName(MessageType messageType)
        => messageType.RuntimeType.FullName ?? messageType.RuntimeType.Name;

    private readonly record struct ChainEntry(InMemoryDestinationKind Kind, string Name, bool IsExplicit);
}
