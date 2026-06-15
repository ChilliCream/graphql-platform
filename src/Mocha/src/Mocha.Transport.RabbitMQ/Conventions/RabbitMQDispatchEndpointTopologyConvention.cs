using Mocha.Features;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Convention that registers exchanges and queues in the topology for dispatch endpoints,
/// merging into any existing entity with the same name using the 3.5 merge rules.
/// Partial exchange declarations contributed via <c>PublishExchange</c> or <c>SendExchange</c>
/// on a message type are materialized here by merging the declared properties onto the resolved
/// convention exchange.
/// </summary>
public sealed class RabbitMQDispatchEndpointTopologyConvention : IRabbitMQDispatchEndpointTopologyConvention
{
    /// <summary>
    /// Discovers and creates missing topology resources (exchanges or queues) needed by the dispatch endpoint.
    /// Binds the endpoint's custom exchange to the resolver's chain entry for each route when in implicit mode,
    /// so the producer and consumer paths converge on the same entity. Also materializes any partial exchange
    /// declarations contributed via the message type's feature bag onto the resolved chain-entry exchange.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="endpoint">The dispatch endpoint being configured.</param>
    /// <param name="configuration">The endpoint configuration specifying the target exchange or queue name.</param>
    public void DiscoverTopology(
        IMessagingConfigurationContext context,
        RabbitMQDispatchEndpoint endpoint,
        RabbitMQDispatchEndpointConfiguration configuration)
    {
        var topology = (RabbitMQMessagingTopology)endpoint.Transport.Topology;

        if (configuration.ExchangeName is not null)
        {
            topology.AddExchange(new RabbitMQExchangeConfiguration { Name = configuration.ExchangeName });
        }

        if (configuration.QueueName is not null)
        {
            topology.AddQueue(new RabbitMQQueueConfiguration
            {
                Name = configuration.QueueName,
                AutoProvision = configuration.AutoProvision
            });
        }

        var resolver = ((RabbitMQMessagingTransport)endpoint.Transport).Resolver;

        // In implicit mode, bind the custom dispatch exchange to the resolver's chain entry for each route
        // so the producer and consumer sides converge on the same entity.
        if (configuration.ExchangeName is not null
            && endpoint.Transport.BindMode == MessagingBindMode.Implicit)
        {
            foreach (var (runtimeType, kind) in configuration.Routes)
            {
                var messageType = context.Messages.GetMessageType(runtimeType);
                if (messageType is null)
                {
                    continue;
                }

                var chainEntry = ResolveChainEntry(context, resolver, messageType, kind);

                // An explicit queue destination has no exchange chain to bridge into.
                if (chainEntry.Kind == RabbitMQDestinationKind.Queue)
                {
                    continue;
                }

                var chainExchangeName = chainEntry.Name;

                if (configuration.ExchangeName == chainExchangeName)
                {
                    continue;
                }

                topology.AddExchange(new RabbitMQExchangeConfiguration { Name = chainExchangeName });

                topology.AddBinding(
                    new RabbitMQBindingConfiguration
                    {
                        Source = configuration.ExchangeName,
                        Destination = chainExchangeName,
                        DestinationKind = RabbitMQDestinationKind.Exchange
                    });
            }
        }

        // Materialize any partial exchange declarations contributed via PublishExchange or SendExchange
        // onto the resolved chain-entry exchange, using the 3.5 merge rules (declared non-null scalar wins).
        // This pass uses the router's outbound routes connected to this endpoint rather than
        // configuration.Routes, because convention dispatch endpoints do not populate Routes.
        foreach (var outboundRoute in context.Router.OutboundRoutes)
        {
            if (outboundRoute.Endpoint != endpoint)
            {
                continue;
            }

            var messageType = outboundRoute.MessageType;
            if (messageType is null)
            {
                continue;
            }

            var contribution = GetContribution(messageType, outboundRoute.Kind);
            if (contribution is null)
            {
                continue;
            }

            var chainEntry = ResolveChainEntry(context, resolver, messageType, outboundRoute.Kind);

            // Contributions are only meaningful for exchange-routed types.
            if (chainEntry.Kind != RabbitMQDestinationKind.Exchange)
            {
                continue;
            }

            contribution.Name = chainEntry.Name;
            topology.AddExchange(contribution);
        }
    }

    private static RabbitMQDestinationResolution ResolveChainEntry(
        IMessagingConfigurationContext context,
        RabbitMQDestinationResolver resolver,
        MessageType messageType,
        OutboundRouteKind kind)
    {
        // Use the outbound route so the resolver can honor any explicit destination.
        var outboundRoute = context.Router.GetOutboundByMessageType(messageType)
            .FirstOrDefault(r => r.Kind == kind);

        if (outboundRoute is not null)
        {
            return resolver.ResolveDestination(context.Naming, outboundRoute);
        }

        // Route not yet registered: fall back to the convention exchange name.
        return kind == OutboundRouteKind.Publish
            ? resolver.ResolvePublishDestination(context.Naming, messageType)
            : new RabbitMQDestinationResolution(
                RabbitMQDestinationKind.Exchange,
                context.Naming.GetSendEndpointName(messageType.RuntimeType),
                "e/" + context.Naming.GetSendEndpointName(messageType.RuntimeType));
    }

    private static RabbitMQExchangeConfiguration? GetContribution(MessageType messageType, OutboundRouteKind kind)
    {
        if (kind == OutboundRouteKind.Publish
            && messageType.Features.TryGet<RabbitMQPublishExchangeFeature>(out var publishFeature))
        {
            return CloneConfiguration(publishFeature.Configuration);
        }

        if (kind == OutboundRouteKind.Send
            && messageType.Features.TryGet<RabbitMQSendExchangeFeature>(out var sendFeature))
        {
            return CloneConfiguration(sendFeature.Configuration);
        }

        return null;
    }

    private static RabbitMQExchangeConfiguration CloneConfiguration(RabbitMQExchangeConfiguration source)
        => new()
        {
            Type = source.Type,
            Durable = source.Durable,
            AutoDelete = source.AutoDelete,
            Arguments = source.Arguments,
            AutoProvision = source.AutoProvision,
            Provenance = source.Provenance
        };
}
