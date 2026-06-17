using Mocha.Features;
using Mocha.Middlewares;
using static System.StringSplitOptions;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Defines the endpoint and topology layout for the RabbitMQ transport.
/// </summary>
public sealed class RabbitMQRoutingStrategy(RabbitMQMessagingTransport transport)
    : RoutingStrategy(transport)
{
    /// <inheritdoc />
    public override DispatchEndpointConfiguration? CreateEndpointConfiguration(
        IMessagingConfigurationContext context,
        OutboundRoute route)
    {
        if (route.Kind is not (OutboundRouteKind.Send or OutboundRouteKind.Publish))
        {
            return null;
        }

        var resolution = transport.Resolver.ResolveDestination(context.Naming, route);

        RabbitMQDispatchEndpointConfiguration configuration;
        if (resolution.Kind == RabbitMQDestinationKind.Queue)
        {
            configuration = new RabbitMQDispatchEndpointConfiguration
            {
                QueueName = resolution.Name,
                Name = resolution.EndpointName
            };
        }
        else
        {
            configuration = new RabbitMQDispatchEndpointConfiguration
            {
                ExchangeName = resolution.Name,
                Name = resolution.EndpointName
            };
        }

        return configuration;
    }

    /// <inheritdoc />
    public override DispatchEndpointConfiguration? CreateEndpointConfiguration(
        IMessagingConfigurationContext context,
        Uri address)
    {
        RabbitMQDispatchEndpointConfiguration? configuration = null;

        var path = address.AbsolutePath.AsSpan();
        Span<Range> ranges = stackalloc Range[2];
        var segmentCount = path.Split(ranges, '/', RemoveEmptyEntries | TrimEntries);

        if (address.Scheme == transport.Schema && address.Host is "")
        {
            if (segmentCount == 1 && path[ranges[0]] is "replies")
            {
                var instanceEndpointName = context.Naming.GetInstanceEndpoint(context.Host.InstanceId);
                configuration = new RabbitMQDispatchEndpointConfiguration
                {
                    Kind = DispatchEndpointKind.Reply,
                    QueueName = instanceEndpointName,
                    Name = "Replies"
                };
            }

            if (segmentCount == 2)
            {
                var kind = path[ranges[0]];
                var name = path[ranges[1]];

                if (kind is "e" && name is var exchangeName)
                {
                    configuration = new RabbitMQDispatchEndpointConfiguration
                    {
                        ExchangeName = new string(exchangeName),
                        Name = "e/" + new string(exchangeName)
                    };
                }

                if (kind is "q" && name is var queueName)
                {
                    configuration = new RabbitMQDispatchEndpointConfiguration
                    {
                        QueueName = new string(queueName),
                        Name = "q/" + new string(queueName),
                        AutoProvision = TryGetSatelliteAutoProvision(address)
                    };
                }
            }
        }

        if (configuration is null && transport.Topology.Address.IsBaseOf(address) && segmentCount == 2)
        {
            var kind = path[ranges[0]];
            var name = path[ranges[1]];

            if (kind is "e" && name is var exchangeName)
            {
                configuration = new RabbitMQDispatchEndpointConfiguration
                {
                    ExchangeName = new string(exchangeName),
                    Name = "e/" + new string(exchangeName)
                };
            }

            if (kind is "q" && name is var queueName)
            {
                configuration = new RabbitMQDispatchEndpointConfiguration
                {
                    QueueName = new string(queueName),
                    Name = "q/" + new string(queueName)
                };
            }
        }

        var isEffectiveDefault = transport.IsDefaultTransport || context.Transports.Length == 1;

        if (configuration is null && isEffectiveDefault && address is { Scheme: "queue" } && segmentCount == 1)
        {
            var name = path[ranges[0]];
            configuration = new RabbitMQDispatchEndpointConfiguration
            {
                QueueName = new string(name),
                Name = "q/" + new string(name)
            };
        }

        if (configuration is null && isEffectiveDefault && address is { Scheme: "exchange" } && segmentCount == 1)
        {
            var name = path[ranges[0]];

            configuration = new RabbitMQDispatchEndpointConfiguration
            {
                ExchangeName = new string(name),
                Name = "e/" + new string(name)
            };
        }

        return configuration;
    }

    /// <inheritdoc />
    public override ReceiveEndpointConfiguration CreateEndpointConfiguration(
        IMessagingConfigurationContext context,
        InboundRoute route)
    {
        RabbitMQReceiveEndpointConfiguration configuration;
        if (route.Kind == InboundRouteKind.Reply)
        {
            var instanceEndpointName = context.Naming.GetInstanceEndpoint(context.Host.InstanceId);
            configuration = new RabbitMQReceiveEndpointConfiguration
            {
                Name = "Replies",
                QueueName = instanceEndpointName,
                IsTemporary = true,
                Kind = ReceiveEndpointKind.Reply,
                AutoProvision = true,
                ReceiveMiddlewares = [ReplyReceiveMiddleware.Create()]
            };
        }
        else
        {
            var queueName = context.Naming.GetReceiveEndpointName(route, ReceiveEndpointKind.Default);
            configuration = new RabbitMQReceiveEndpointConfiguration { Name = queueName, QueueName = queueName };
        }

        return configuration;
    }

    /// <inheritdoc />
    public override void DiscoverTopology(
        IMessagingConfigurationContext context,
        ReceiveEndpoint endpoint,
        ReceiveEndpointConfiguration configuration)
    {
        if (endpoint is RabbitMQReceiveEndpoint rabbitEndpoint
            && configuration is RabbitMQReceiveEndpointConfiguration rabbitConfiguration)
        {
            DiscoverReceiveTopology(context, rabbitEndpoint, rabbitConfiguration);
        }
    }

    /// <inheritdoc />
    public override void DiscoverTopology(
        IMessagingConfigurationContext context,
        DispatchEndpoint endpoint,
        DispatchEndpointConfiguration configuration)
    {
        if (endpoint is RabbitMQDispatchEndpoint rabbitEndpoint
            && configuration is RabbitMQDispatchEndpointConfiguration rabbitConfiguration)
        {
            DiscoverDispatchTopology(context, rabbitEndpoint, rabbitConfiguration);
        }
    }

    internal static void DiscoverReceiveTopology(
        IMessagingConfigurationContext context,
        RabbitMQReceiveEndpoint endpoint,
        RabbitMQReceiveEndpointConfiguration configuration)
    {
        if (configuration.QueueName is null)
        {
            throw new InvalidOperationException("Queue name is required");
        }

        var topology = (RabbitMQMessagingTopology)endpoint.Transport.Topology;

        topology.AddQueue(
            new RabbitMQQueueConfiguration
            {
                Name = configuration.QueueName,
                AutoDelete = endpoint.Kind == ReceiveEndpointKind.Reply,
                AutoProvision = configuration.AutoProvision,
                Provenance = RabbitMQTopologyProvenance.Endpoint
            });

        if (endpoint.Kind is ReceiveEndpointKind.Reply or ReceiveEndpointKind.Error or ReceiveEndpointKind.Skipped)
        {
            return;
        }

        var resolver = ((RabbitMQMessagingTransport)endpoint.Transport).Resolver;

        var routes = context.Router.GetInboundByEndpoint(endpoint);
        foreach (var route in routes)
        {
            if (route.Kind is InboundRouteKind.Reply)
            {
                continue;
            }

            if (route.MessageType is null)
            {
                continue;
            }

            var autoBind = ResolveAutoBind(endpoint.Transport, configuration);

            if (autoBind
                && resolver.ResolveBindKey(route.MessageType).Kind is RabbitMQBindKeyKind.Underivable)
            {
                throw ThrowHelper.ConsumeBindUnderivable(GetTypeName(route.MessageType), configuration.QueueName);
            }

            var chainEntry = ResolveChainEntry(context, resolver, route.MessageType);

            if (chainEntry.Kind == RabbitMQDestinationKind.Queue)
            {
                if (autoBind)
                {
                    throw ThrowHelper.ConsumeBindUnderivable(GetTypeName(route.MessageType), configuration.QueueName);
                }

                continue;
            }

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

            var sendExchangeName = context.Naming.GetSendEndpointName(route.MessageType.RuntimeType);
            if (sendExchangeName != publishExchangeName)
            {
                EnsureExchange(topology, sendExchangeName);

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

            if (autoBind)
            {
                EnsureExchangeToQueueBinding(topology, sendExchangeName, configuration.QueueName);
            }
        }
    }

    internal static void DiscoverDispatchTopology(
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

            if (chainEntry.Kind != RabbitMQDestinationKind.Exchange)
            {
                continue;
            }

            contribution.Name = chainEntry.Name;
            topology.AddExchange(contribution);
        }
    }

    private bool? TryGetSatelliteAutoProvision(Uri address)
    {
        if (transport.Configuration is null)
        {
            return null;
        }

        foreach (var receiveEndpoint in transport.Configuration.ReceiveEndpoints)
        {
            if (receiveEndpoint is not RabbitMQReceiveEndpointConfiguration rabbitReceiveEndpoint)
            {
                continue;
            }

            if (rabbitReceiveEndpoint.ErrorEndpoint == address)
            {
                return rabbitReceiveEndpoint.ErrorQueue.AutoProvision;
            }

            if (rabbitReceiveEndpoint.SkippedEndpoint == address)
            {
                return rabbitReceiveEndpoint.SkippedQueue.AutoProvision;
            }
        }

        return null;
    }

    private static bool ResolveAutoBind(
        MessagingTransport transport,
        RabbitMQReceiveEndpointConfiguration configuration)
    {
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

    private static RabbitMQDestinationResolution ResolveChainEntry(
        IMessagingConfigurationContext context,
        RabbitMQDestinationResolver resolver,
        MessageType messageType,
        OutboundRouteKind kind)
    {
        var outboundRoute = context.Router.GetOutboundByMessageType(messageType)
            .FirstOrDefault(r => r.Kind == kind);

        if (outboundRoute is not null)
        {
            return resolver.ResolveDestination(context.Naming, outboundRoute);
        }

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
