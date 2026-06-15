namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Convention that creates the exchange and binding chains in the topology for receive endpoints,
/// building the publish and send exchange hierarchy and binding it to the endpoint's queue for each
/// inbound route whose auto-binding is enabled. The endpoint owns its queue, so this convention binds
/// to it but never creates it. Auto-binding is resolved per route with the queue, transport
/// precedence; when it is off, the bind into this queue is suppressed while the type-owned publish and
/// send exchanges are still produced.
/// </summary>
public sealed class RabbitMQReceiveEndpointTopologyConvention : IRabbitMQReceiveEndpointTopologyConvention
{
    /// <summary>
    /// Discovers and creates the missing exchanges and bindings needed by the receive endpoint based on
    /// its inbound message routes, binding them to the endpoint's existing queue. The chain entry is
    /// resolved through the transport's destination resolver so the producer and consumer paths converge
    /// on the same entity.
    /// </summary>
    /// <param name="context">The messaging configuration context providing naming and routing information.</param>
    /// <param name="endpoint">The receive endpoint being configured.</param>
    /// <param name="configuration">The endpoint configuration specifying the source queue name.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the queue name is not set on the configuration, or if a consumed type's bind into the
    /// queue cannot be derived because its routing key is computed per message or because the type names
    /// an explicit queue destination.
    /// </exception>
    public void DiscoverTopology(
        IMessagingConfigurationContext context,
        RabbitMQReceiveEndpoint endpoint,
        RabbitMQReceiveEndpointConfiguration configuration)
    {
        if (configuration.QueueName is null)
        {
            throw new InvalidOperationException("Queue name is required");
        }

        var topology = (RabbitMQMessagingTopology)endpoint.Transport.Topology;

        if (endpoint.Kind is ReceiveEndpointKind.Reply or ReceiveEndpointKind.Error or ReceiveEndpointKind.Skipped)
        {
            return;
        }

        var resolver = ((RabbitMQMessagingTransport)endpoint.Transport).Resolver;

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

            // Auto-binding is resolved per route with the queue > transport precedence.
            // When it is off, the only effect is that no convention bind into this queue is generated
            // for the type; the type-owned publish/send exchanges are still built so a second endpoint
            // that does auto-bind the same type keeps a complete chain (suppression scope).
            var autoBind = ResolveAutoBind(endpoint.Transport, configuration);

            // The bind-key and queue-destination diagnostics only fire when a bind is actually being
            // derived. With auto-binding off no bind is derived, so an underivable key or an explicit
            // queue destination is not a build failure for this queue.
            if (autoBind
                && resolver.ResolveBindKey(route.MessageType).Kind is RabbitMQBindKeyKind.Underivable)
            {
                throw ThrowHelper.ConsumeBindUnderivable(GetTypeName(route.MessageType), configuration.QueueName);
            }

            var chainEntry = ResolveChainEntry(context, resolver, route.MessageType);

            // An explicit queue destination on a consumed type has no exchange chain to bind into the
            // endpoint queue, so the bind is underivable. Fail the build instead of guessing.
            if (chainEntry.Kind == RabbitMQDestinationKind.Queue)
            {
                if (autoBind)
                {
                    throw ThrowHelper.ConsumeBindUnderivable(GetTypeName(route.MessageType), configuration.QueueName);
                }

                continue;
            }

            // An explicitly named exchange is bound directly into the endpoint queue; the convention
            // publish/send sublayer applies only to convention-named destinations.
            if (chainEntry.IsExplicit)
            {
                EnsureExchange(topology, chainEntry.Name);

                if (autoBind)
                {
                    EnsureExchangeToQueueBinding(topology, chainEntry.Name, configuration.QueueName);
                }

                continue;
            }

            var publishExchangeName = chainEntry.Name;
            EnsureExchange(topology, publishExchangeName);

            // make sure the exchange for the message type exists
            var sendExchangeName = context.Naming.GetSendEndpointName(route.MessageType.RuntimeType);
            if (sendExchangeName != publishExchangeName)
            {
                EnsureExchange(topology, sendExchangeName);

                // make sure the binding between the publish exchange and the send exchange exists
                if (topology.Bindings.FirstOrDefault(b =>
                        b.Source.Name == publishExchangeName
                        && string.IsNullOrEmpty(b.RoutingKey)
                        && b is RabbitMQExchangeBinding exchangeBinding
                        && exchangeBinding.Destination.Name == sendExchangeName
                    )
                    is null)
                {
                    topology.AddBinding(
                        new RabbitMQBindingConfiguration
                        {
                            Source = publishExchangeName,
                            Destination = sendExchangeName,
                            DestinationKind = RabbitMQDestinationKind.Exchange
                        });
                }
            }

            // The bind into this queue is the only artifact auto-binding gates; the exchange chain above
            // is type-owned and remains so a second endpoint binding the same type stays complete.
            if (autoBind)
            {
                EnsureExchangeToQueueBinding(topology, sendExchangeName, configuration.QueueName);
            }
        }
    }

    private static bool ResolveAutoBind(
        MessagingTransport transport,
        RabbitMQReceiveEndpointConfiguration configuration)
    {
        // Queue scope wins over transport scope.
        if (configuration.BindMode.HasValue)
        {
            return configuration.BindMode.Value == MessagingBindMode.Implicit;
        }

        return transport.BindMode == MessagingBindMode.Implicit;
    }

    private static ChainEntry ResolveChainEntry(
        IMessagingConfigurationContext context,
        RabbitMQDestinationResolver resolver,
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

        var conventionExchange = resolver.ResolvePublishDestination(context.Naming, messageType);
        return new ChainEntry(conventionExchange.Kind, conventionExchange.Name, IsExplicit: false);
    }

    private static void EnsureExchange(RabbitMQMessagingTopology topology, string exchangeName)
    {
        if (topology.Exchanges.FirstOrDefault(e => e.Name == exchangeName) is null)
        {
            topology.AddExchange(new RabbitMQExchangeConfiguration { Name = exchangeName });
        }
    }

    private static void EnsureExchangeToQueueBinding(
        RabbitMQMessagingTopology topology,
        string sourceExchangeName,
        string queueName)
    {
        if (topology.Bindings.FirstOrDefault(b =>
                b.Source.Name == sourceExchangeName
                && string.IsNullOrEmpty(b.RoutingKey)
                && b is RabbitMQExchangeBinding exchangeBinding
                && exchangeBinding.Destination.Name == queueName
            )
            is null)
        {
            topology.AddBinding(
                new RabbitMQBindingConfiguration
                {
                    Source = sourceExchangeName,
                    Destination = queueName,
                    DestinationKind = RabbitMQDestinationKind.Queue
                });
        }
    }

    private static string GetTypeName(MessageType messageType)
        => messageType.RuntimeType.FullName ?? messageType.RuntimeType.Name;

    private readonly record struct ChainEntry(RabbitMQDestinationKind Kind, string Name, bool IsExplicit);
}
