using Mocha.Features;
using Mocha.Middlewares;
using static System.StringSplitOptions;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Defines the endpoint and topology layout for the RabbitMQ transport.
/// </summary>
public sealed class RabbitMQRoutingStrategy : RoutingStrategy<RabbitMQMessagingTransport>
{
    private RabbitMQMessagingTopology _topology = null!;

    protected override void OnInitialize(RabbitMQMessagingTransport transport)
    {
        _topology = (RabbitMQMessagingTopology)transport.Topology;
    }

    /// <inheritdoc />
    public override DispatchEndpointConfiguration? CreateEndpointConfiguration(
        IMessagingConfigurationContext context,
        OutboundRoute route)
    {
        if (route.Kind is not (OutboundRouteKind.Send or OutboundRouteKind.Publish))
        {
            return null;
        }

        var resolution = RabbitMQDestinations.Resolve(Transport.Schema, context.Naming, route);

        if (resolution.Kind == RabbitMQDestinationKind.Queue)
        {
            return new RabbitMQDispatchEndpointConfiguration
            {
                QueueName = resolution.Name,
                Name = resolution.EndpointName
            };
        }

        return new RabbitMQDispatchEndpointConfiguration
        {
            ExchangeName = resolution.Name,
            Name = resolution.EndpointName
        };
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

        if (address.Scheme == Transport.Schema && address.Host is "")
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

        if (configuration is null && Transport.Topology.Address.IsBaseOf(address) && segmentCount == 2)
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

        var isEffectiveDefault = Transport.IsDefaultTransport || context.Transports.Length == 1;

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
        if (route.Kind == InboundRouteKind.Reply)
        {
            var instanceEndpointName = context.Naming.GetInstanceEndpoint(context.Host.InstanceId);
            return new RabbitMQReceiveEndpointConfiguration
            {
                Name = "Replies",
                QueueName = instanceEndpointName,
                IsTemporary = true,
                Kind = ReceiveEndpointKind.Reply,
                AutoProvision = true,
                ReceiveMiddlewares = [ReplyReceiveMiddleware.Create()]
            };
        }

        var queueName = context.Naming.GetReceiveEndpointName(route, ReceiveEndpointKind.Default);
        return new RabbitMQReceiveEndpointConfiguration { Name = queueName, QueueName = queueName };
    }

    /// <inheritdoc />
    public override void DiscoverTopology(
        IMessagingConfigurationContext context,
        ReceiveEndpoint endpoint,
        ReceiveEndpointConfiguration configuration)
    {
        if (endpoint is not RabbitMQReceiveEndpoint rabbitEndpoint
            || configuration is not RabbitMQReceiveEndpointConfiguration rabbitConfiguration)
        {
            return;
        }

        if (rabbitConfiguration.QueueName is null)
        {
            throw new InvalidOperationException("Queue name is required");
        }

        _topology.AddQueue(
            new RabbitMQQueueConfiguration
            {
                Name = rabbitConfiguration.QueueName,
                AutoDelete = rabbitEndpoint.Kind == ReceiveEndpointKind.Reply,
                AutoProvision = rabbitConfiguration.AutoProvision,
                Provenance = RabbitMQTopologyProvenance.Endpoint
            });

        if (rabbitEndpoint.Kind is ReceiveEndpointKind.Reply or ReceiveEndpointKind.Error or ReceiveEndpointKind.Skipped)
        {
            return;
        }

        var schema = rabbitEndpoint.Transport.Schema;
        var autoBind = (rabbitConfiguration.BindMode ?? rabbitEndpoint.Transport.BindMode)
            is MessagingBindMode.Implicit;

        var routes = context.Router.GetInboundByEndpoint(rabbitEndpoint);
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

            if (!autoBind || HasPerMessageRoutingKey(route.MessageType))
            {
                continue;
            }

            var explicitPublishRoute = context.Router.GetOutboundByMessageType(route.MessageType)
                .FirstOrDefault(r => r is { HasExplicitDestination: true, Kind: OutboundRouteKind.Publish });

            if (explicitPublishRoute is not null)
            {
                var destination = RabbitMQDestinations.Resolve(schema, context.Naming, explicitPublishRoute);

                if (destination.Kind == RabbitMQDestinationKind.Queue)
                {
                    continue;
                }

                EnsureExchange(_topology, destination.Name);
                EnsureExchangeToQueueBinding(_topology, destination.Name, rabbitConfiguration.QueueName);

                continue;
            }

            // Convention routing fans publish traffic into the send exchange, then binds the send
            // exchange into the receive queue. This keeps Publish<T> and Send<T> converged on the
            // same queue while still allowing separate publish and send exchange names.
            // Example: publish/order-created -> send/order-created -> queue/orders.
            var publishExchangeName = context.Naming.GetPublishEndpointName(route.MessageType.RuntimeType);
            EnsureExchange(_topology, publishExchangeName);

            var sendExchangeName = context.Naming.GetSendEndpointName(route.MessageType.RuntimeType);
            if (sendExchangeName != publishExchangeName)
            {
                EnsureExchange(_topology, sendExchangeName);

                if (_topology.Bindings.FirstOrDefault(b =>
                        b.Source.Name == publishExchangeName
                        && string.IsNullOrEmpty(b.RoutingKey)
                        && b is RabbitMQExchangeBinding exchangeBinding
                        && exchangeBinding.Destination.Name == sendExchangeName
                    )
                    is null)
                {
                    _topology.AddBinding(
                        new RabbitMQBindingConfiguration
                        {
                            Source = publishExchangeName,
                            Destination = sendExchangeName,
                            DestinationKind = RabbitMQDestinationKind.Exchange
                        });
                }
            }

            EnsureExchangeToQueueBinding(_topology, sendExchangeName, rabbitConfiguration.QueueName);
        }
    }

    /// <inheritdoc />
    public override void DiscoverTopology(
        IMessagingConfigurationContext context,
        DispatchEndpoint endpoint,
        DispatchEndpointConfiguration configuration)
    {
        if (endpoint is not RabbitMQDispatchEndpoint
            || configuration is not RabbitMQDispatchEndpointConfiguration rabbitConfiguration)
        {
            return;
        }

        if (rabbitConfiguration.ExchangeName is not null)
        {
            _topology.AddExchange(new RabbitMQExchangeConfiguration { Name = rabbitConfiguration.ExchangeName });
        }

        if (rabbitConfiguration.QueueName is not null)
        {
            _topology.AddQueue(new RabbitMQQueueConfiguration
            {
                Name = rabbitConfiguration.QueueName,
                AutoProvision = rabbitConfiguration.AutoProvision
            });
        }

        var schema = Transport.Schema;

        if (rabbitConfiguration.ExchangeName is not null
            && Transport.BindMode == MessagingBindMode.Implicit)
        {
            foreach (var (runtimeType, kind) in rabbitConfiguration.Routes)
            {
                var messageType = context.Messages.GetMessageType(runtimeType);
                if (messageType is null)
                {
                    continue;
                }

                var outboundRoute = context.Router.GetOutboundByMessageType(messageType)
                    .FirstOrDefault(r => r.Kind == kind);
                var destination = outboundRoute is not null
                    ? RabbitMQDestinations.Resolve(schema, context.Naming, outboundRoute)
                    : RabbitMQDestinations.ResolveConvention(context.Naming, kind, messageType);

                if (destination.Kind == RabbitMQDestinationKind.Queue)
                {
                    continue;
                }

                var exchangeName = destination.Name;

                if (rabbitConfiguration.ExchangeName == exchangeName)
                {
                    continue;
                }

                _topology.AddExchange(new RabbitMQExchangeConfiguration { Name = exchangeName });

                _topology.AddBinding(
                    new RabbitMQBindingConfiguration
                    {
                        Source = rabbitConfiguration.ExchangeName,
                        Destination = exchangeName,
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

            var destination = RabbitMQDestinations.Resolve(schema, context.Naming, outboundRoute);

            if (destination.Kind != RabbitMQDestinationKind.Exchange)
            {
                continue;
            }

            contribution.Name = destination.Name;
            _topology.AddExchange(contribution);
        }
    }

    private bool? TryGetSatelliteAutoProvision(Uri address)
    {
        if (Transport.Configuration is null)
        {
            return null;
        }

        foreach (var receiveEndpoint in Transport.Configuration.ReceiveEndpoints)
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

    private static bool HasPerMessageRoutingKey(MessageType messageType)
        => messageType.Features.TryGet<RabbitMQRoutingKeyExtractor>(out _);
}
