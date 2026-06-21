using Mocha.Features;
using Mocha.Middlewares;
using static System.StringSplitOptions;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Defines the endpoint and topology layout for the RabbitMQ transport.
/// </summary>
public sealed class RabbitMQRoutingStrategy : RoutingStrategy<RabbitMQMessagingTransport>
{
    private RabbitMQMessagingTopology _topology =>
        field ??= (RabbitMQMessagingTopology)Transport.Topology;

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
                    var queueNameValue = new string(queueName);
                    configuration = new RabbitMQDispatchEndpointConfiguration
                    {
                        QueueName = queueNameValue,
                        Name = "q/" + queueNameValue,
                        AutoProvision = GetQueueAutoProvision(queueNameValue)
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
                var queueNameValue = new string(queueName);
                configuration = new RabbitMQDispatchEndpointConfiguration
                {
                    QueueName = queueNameValue,
                    Name = "q/" + queueNameValue,
                    AutoProvision = GetQueueAutoProvision(queueNameValue)
                };
            }
        }

        if (configuration is null
            && Transport.Topology.Address.IsBaseOf(address)
            && TryGetBaseQueueName(address, out var baseQueueName))
        {
            configuration = new RabbitMQDispatchEndpointConfiguration
            {
                QueueName = baseQueueName,
                Name = "q/" + baseQueueName,
                AutoProvision = GetQueueAutoProvision(baseQueueName)
            };
        }

        var isEffectiveDefault = Transport.IsDefaultTransport || context.Transports.Length == 1;

        if (configuration is null && isEffectiveDefault && address is { Scheme: "queue" } && segmentCount == 1)
        {
            var name = new string(path[ranges[0]]);
            configuration = new RabbitMQDispatchEndpointConfiguration
            {
                QueueName = name,
                Name = "q/" + name,
                AutoProvision = GetQueueAutoProvision(name)
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

    public override void ConfigureEndpoint(
        IMessagingConfigurationContext context,
        ReceiveEndpointConfiguration configuration)
    {
        if (configuration is not RabbitMQReceiveEndpointConfiguration rabbitConfiguration)
        {
            return;
        }

        rabbitConfiguration.QueueName ??= rabbitConfiguration.Name;

        if (rabbitConfiguration is { Kind: ReceiveEndpointKind.Default, QueueName: { } queueName })
        {
            var faultFeature = rabbitConfiguration.Features.GetOrSet<ReceiveFaultEndpointFeature>();
            ConfigureFaultOrSkippedEndpoint(
                context,
                queueName,
                ReceiveEndpointKind.Error,
                faultFeature,
                endpoint => faultFeature.Address ??= endpoint);

            var skippedFeature = rabbitConfiguration.Features.GetOrSet<ReceiveSkippedEndpointFeature>();
            ConfigureFaultOrSkippedEndpoint(
                context,
                queueName,
                ReceiveEndpointKind.Skipped,
                skippedFeature,
                endpoint => skippedFeature.Address ??= endpoint);
        }
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

        _topology.GetOrAddQueue(
            rabbitConfiguration.QueueName,
            _ => new RabbitMQQueueConfiguration
            {
                AutoDelete = rabbitEndpoint.Kind == ReceiveEndpointKind.Reply,
                AutoProvision = rabbitConfiguration.AutoProvision,
                Origin = TopologyOrigin.Endpoint
            });

        if (rabbitEndpoint.Kind == ReceiveEndpointKind.Default)
        {
            var inheritedAutoProvision = GetInheritedQueueAutoProvision(
                rabbitConfiguration.QueueName,
                rabbitConfiguration);

            EnsureFaultOrSkippedQueue(
                context,
                rabbitConfiguration.Features.Get<ReceiveFaultEndpointFeature>()?.Address,
                inheritedAutoProvision);
            EnsureFaultOrSkippedQueue(
                context,
                rabbitConfiguration.Features.Get<ReceiveSkippedEndpointFeature>()?.Address,
                inheritedAutoProvision);
        }

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

                _topology.EnsureBinding(
                    publishExchangeName,
                    sendExchangeName,
                    RabbitMQDestinationKind.Exchange,
                    static (_, _, _) => new RabbitMQBindingConfiguration());
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
            _topology.GetOrAddExchange(
                rabbitConfiguration.ExchangeName,
                static _ => new RabbitMQExchangeConfiguration());
        }

        if (rabbitConfiguration.QueueName is not null)
        {
            _topology.GetOrAddQueue(
                rabbitConfiguration.QueueName,
                _ => new RabbitMQQueueConfiguration { AutoProvision = rabbitConfiguration.AutoProvision });
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

                _topology.GetOrAddExchange(
                    exchangeName,
                    static _ => new RabbitMQExchangeConfiguration());

                _topology.EnsureBinding(
                    rabbitConfiguration.ExchangeName,
                    exchangeName,
                    RabbitMQDestinationKind.Exchange,
                    static (_, _, _) => new RabbitMQBindingConfiguration());
            }
        }
    }

    private bool? GetQueueAutoProvision(string queueName)
        => _topology.Queues.FirstOrDefault(q => q.Name == queueName)?.AutoProvision
            ?? (Transport.Configuration as RabbitMQTransportConfiguration)
                ?.Queues.FirstOrDefault(q => q.Name == queueName)?.AutoProvision;

    private static void EnsureExchange(RabbitMQMessagingTopology topology, string exchangeName)
    {
        if (topology.Exchanges.FirstOrDefault(e => e.Name == exchangeName) is null)
        {
            topology.GetOrAddExchange(
                exchangeName,
                static _ => new RabbitMQExchangeConfiguration());
        }
    }

    private static void EnsureExchangeToQueueBinding(
        RabbitMQMessagingTopology topology,
        string sourceExchangeName,
        string queueName)
    {
        topology.EnsureBinding(
            sourceExchangeName,
            queueName,
            RabbitMQDestinationKind.Queue,
            static (_, _, _) => new RabbitMQBindingConfiguration());
    }

    private void ConfigureFaultOrSkippedEndpoint(
        IMessagingConfigurationContext context,
        string queueName,
        ReceiveEndpointKind kind,
        ReceiveFaultEndpointFeature feature,
        Action<Uri> assign)
    {
        if (feature.IsDisabled)
        {
            return;
        }

        if (feature.Address is null)
        {
            var name = context.Naming.GetReceiveEndpointName(queueName, kind);
            assign(new Uri($"{Transport.Schema}:q/{name}"));
        }
    }

    private void ConfigureFaultOrSkippedEndpoint(
        IMessagingConfigurationContext context,
        string queueName,
        ReceiveEndpointKind kind,
        ReceiveSkippedEndpointFeature feature,
        Action<Uri> assign)
    {
        if (feature.IsDisabled)
        {
            return;
        }

        if (feature.Address is null)
        {
            var name = context.Naming.GetReceiveEndpointName(queueName, kind);
            assign(new Uri($"{Transport.Schema}:q/{name}"));
        }
    }

    private void EnsureFaultOrSkippedQueue(
        IMessagingConfigurationContext context,
        Uri? address,
        bool? inheritedAutoProvision)
    {
        if (address is null || !TryGetQueueName(context, address, out var queueName))
        {
            return;
        }

        var existingQueue = _topology.Queues.FirstOrDefault(q => q.Name == queueName);
        if (existingQueue is not null)
        {
            return;
        }

        _topology.GetOrAddQueue(
            queueName,
            _ => new RabbitMQQueueConfiguration
            {
                AutoProvision = inheritedAutoProvision,
                Origin = TopologyOrigin.Endpoint
            });
    }

    private bool? GetInheritedQueueAutoProvision(
        string queueName,
        RabbitMQReceiveEndpointConfiguration configuration)
        => (Transport.Configuration as RabbitMQTransportConfiguration)
            ?.Queues.FirstOrDefault(q => q.Name == queueName)?.AutoProvision
            ?? configuration.AutoProvision;

    private bool TryGetQueueName(
        IMessagingConfigurationContext context,
        Uri address,
        out string queueName)
    {
        var path = address.AbsolutePath.AsSpan();
        Span<Range> ranges = stackalloc Range[2];
        var segmentCount = path.Split(ranges, '/', RemoveEmptyEntries | TrimEntries);

        if (address.Scheme == Transport.Schema && address.Host is "" && segmentCount == 2)
        {
            var kind = path[ranges[0]];
            if (kind is "q")
            {
                queueName = new string(path[ranges[1]]);
                return true;
            }
        }

        if (Transport.Topology.Address.IsBaseOf(address) && TryGetBaseQueueName(address, out queueName))
        {
            return true;
        }

        var isEffectiveDefault = Transport.IsDefaultTransport || context.Transports.Length == 1;
        if (isEffectiveDefault && address is { Scheme: "queue" } && segmentCount == 1)
        {
            queueName = new string(path[ranges[0]]);
            return true;
        }

        queueName = string.Empty;
        return false;
    }

    private bool TryGetBaseQueueName(Uri address, out string queueName)
    {
        var relative = Transport.Topology.Address.MakeRelativeUri(address);
        if (relative.IsAbsoluteUri)
        {
            queueName = string.Empty;
            return false;
        }

        var relativePath = Uri.UnescapeDataString(relative.GetComponents(UriComponents.Path, UriFormat.Unescaped));
        var path = relativePath.AsSpan();
        Span<Range> ranges = stackalloc Range[2];
        var segmentCount = path.Split(ranges, '/', RemoveEmptyEntries | TrimEntries);

        if (segmentCount == 2 && path[ranges[0]] is "q")
        {
            queueName = new string(path[ranges[1]]);
            return true;
        }

        queueName = string.Empty;
        return false;
    }

    private static bool HasPerMessageRoutingKey(MessageType messageType)
        => messageType.Features.TryGet<RabbitMQRoutingKeyExtractor>(out _);
}
