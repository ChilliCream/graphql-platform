using Mocha.Features;
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

        if (configuration is null
            && TryParseTopologyAddress(address, out var topologyKind, out var resourceName))
        {
            if (topologyKind is 'e')
            {
                configuration = new RabbitMQDispatchEndpointConfiguration
                {
                    ExchangeName = resourceName,
                    Name = "e/" + resourceName
                };
            }
            else if (topologyKind is 'q')
            {
                configuration = new RabbitMQDispatchEndpointConfiguration
                {
                    QueueName = resourceName,
                    Name = "q/" + resourceName,
                    AutoProvision = GetQueueAutoProvision(resourceName)
                };
            }
        }

        if (configuration is null && address is { Scheme: "queue" } && segmentCount == 1)
        {
            var name = new string(path[ranges[0]]);
            configuration = new RabbitMQDispatchEndpointConfiguration
            {
                QueueName = name,
                Name = "q/" + name,
                AutoProvision = GetQueueAutoProvision(name)
            };
        }

        if (configuration is null && address is { Scheme: "exchange" } && segmentCount == 1)
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
                rabbitConfiguration.Features.Get<ReceiveFaultEndpointFeature>()?.Address,
                inheritedAutoProvision);
            EnsureFaultOrSkippedQueue(
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

        foreach (var route in context.Router.GetInboundByEndpoint(rabbitEndpoint))
        {
            if (route.Kind is InboundRouteKind.Reply)
            {
                continue;
            }

            if (route.MessageType is not { } messageType)
            {
                continue;
            }

            if (messageType.HasPerMessageRoutingKey())
            {
                continue;
            }

            var explicitPublishRoute = context.Router.GetOutboundByMessageType(messageType)
                .FirstOrDefault(r => r is { HasExplicitDestination: true, Kind: OutboundRouteKind.Publish });

            if (explicitPublishRoute is not null)
            {
                var destination = RabbitMQDestinations.Resolve(schema, context.Naming, explicitPublishRoute);

                if (destination.Kind == RabbitMQDestinationKind.Queue)
                {
                    continue;
                }

                _topology.EnsureExchange(destination.Name);

                if (autoBind)
                {
                    _topology.EnsureExchangeToQueueBinding(destination.Name, rabbitConfiguration.QueueName);
                }

                continue;
            }

            // Convention routing fans publish traffic into the send exchange, then binds the send
            // exchange into the receive queue. This keeps Publish<T> and Send<T> converged on the
            // same queue while still allowing separate publish and send exchange names.
            // Example: publish/order-created -> send/order-created -> queue/orders.
            //
            // Skip the whole convention pair when the endpoint binds explicitly: the upstream
            // publish/send exchanges only exist to feed the exchange-to-queue binding below, so
            // without that final hop they would be declared on this service but bound to nothing,
            // contradicting the "explicit means explicit" contract.
            if (!autoBind)
            {
                continue;
            }

            var publishExchangeName = context.Naming.GetPublishEndpointName(messageType.RuntimeType);
            _topology.EnsureExchange(publishExchangeName);

            var sendExchangeName = context.Naming.GetSendEndpointName(messageType.RuntimeType);
            if (sendExchangeName != publishExchangeName)
            {
                _topology.EnsureExchange(sendExchangeName);

                _topology.EnsureBinding(
                    publishExchangeName,
                    sendExchangeName,
                    RabbitMQDestinationKind.Exchange,
                    static (_, _, _) => new RabbitMQBindingConfiguration());
            }

            _topology.EnsureExchangeToQueueBinding(sendExchangeName, rabbitConfiguration.QueueName);
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

        // Under BindExplicitly, the dispatch endpoint must not materialize a destination
        // exchange the user never declared: the user retains full ownership of the topology,
        // and a default-config (fanout) shadow can collide with a previously-declared exchange
        // on the broker (PRECONDITION_FAILED on declare). The user is expected to either
        // DeclareExchange it explicitly or rely on a pre-existing broker entity.
        var bindImplicitly = Transport.BindMode == MessagingBindMode.Implicit;

        if (rabbitConfiguration.ExchangeName is not null && bindImplicitly)
        {
            _topology.GetOrAddExchange(
                rabbitConfiguration.ExchangeName,
                static _ => new RabbitMQExchangeConfiguration());
        }

        if (rabbitConfiguration.QueueName is not null && bindImplicitly)
        {
            _topology.GetOrAddQueue(
                rabbitConfiguration.QueueName,
                _ => new RabbitMQQueueConfiguration { AutoProvision = rabbitConfiguration.AutoProvision });
        }

        var schema = Transport.Schema;

        if (rabbitConfiguration.ExchangeName is not null
            && bindImplicitly)
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
        Uri? address,
        bool? inheritedAutoProvision)
    {
        if (address is null || !TryGetQueueName(address, out var queueName))
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
                AutoProvision = inheritedAutoProvision
            });
    }

    private bool? GetInheritedQueueAutoProvision(
        string queueName,
        RabbitMQReceiveEndpointConfiguration configuration)
        => (Transport.Configuration as RabbitMQTransportConfiguration)
            ?.Queues.FirstOrDefault(q => q.Name == queueName)?.AutoProvision
            ?? configuration.AutoProvision;

    private bool TryGetQueueName(
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

        if (TryParseTopologyAddress(address, out var topologyKind, out var topologyName)
            && topologyKind is 'q')
        {
            queueName = topologyName;
            return true;
        }

        if (address is { Scheme: "queue" } && segmentCount == 1)
        {
            queueName = new string(path[ranges[0]]);
            return true;
        }

        queueName = string.Empty;
        return false;
    }

    /// <summary>
    /// Parses an address under the transport topology into its kind and resource name.
    /// Everything after the kind segment is the name, because a queue or exchange name may
    /// contain a slash.
    /// </summary>
    private bool TryParseTopologyAddress(
        Uri address,
        out char kind,
        out string name)
    {
        kind = default;
        name = string.Empty;

        // A valid resource path is not enough to identify this transport. For example,
        // rabbitmq://other-host:5672/tenant-a/q/orders must not be claimed by this topology.
        var topologyAddress = Transport.Topology.Address;
        if (!address.Scheme.EqualsOrdinalIgnoreCase(topologyAddress.Scheme)
            || !address.Host.EqualsOrdinalIgnoreCase(topologyAddress.Host)
            || address.Port != topologyAddress.Port)
        {
            return false;
        }

        // Normalize only the topology path: "/" becomes empty and "/tenant-a/" becomes
        // "/tenant-a". Keep the address path unchanged while removing the topology prefix below.
        var basePath = topologyAddress.AbsolutePath.AsSpan().TrimEnd('/');
        var path = address.AbsolutePath.AsSpan();
        ReadOnlySpan<char> relativePath;

        if (basePath.IsEmpty)
        {
            // A root topology consumes exactly one structural slash:
            // "/q/orders" becomes "q/orders". The shorthand "rabbitmq:q/orders" has no
            // leading slash and is handled by the transport-specific branch above.
            if (path.IsEmpty || path[0] is not '/')
            {
                return false;
            }

            relativePath = path[1..];
        }
        else
        {
            // Remove the exact virtual-host path and its separator:
            // "/tenant-a/q/orders" becomes "q/orders". Requiring the separator prevents
            // "/tenant-a" from matching "/tenant-a-other/q/orders".
            if (path.Length <= basePath.Length
                || !path.StartsWith(basePath, StringComparison.Ordinal)
                || path[basePath.Length] is not '/')
            {
                return false;
            }

            relativePath = path[(basePath.Length + 1)..];
        }

        // Require "{kind}/{name}", where kind is one character. Everything after the first
        // slash is the name, so a name containing a slash, such as "q/nested/queue", stays intact.
        if (relativePath.Length < 3 || relativePath[1] is not '/')
        {
            return false;
        }

        kind = relativePath[0];
        // AbsolutePath is escaped, so "q/space%20name" must produce "space name".
        name = Uri.UnescapeDataString(relativePath[2..].ToString());
        return true;
    }
}

file static class Extensions
{
    public static bool HasPerMessageRoutingKey(this MessageType messageType)
        => messageType.Features.TryGet<RabbitMQRoutingKeyExtractor>(out _);

    public static void EnsureExchange(
        this RabbitMQMessagingTopology topology,
        string exchangeName)
    {
        if (topology.Exchanges.FirstOrDefault(e => e.Name == exchangeName) is null)
        {
            topology.GetOrAddExchange(
                exchangeName,
                static _ => new RabbitMQExchangeConfiguration());
        }
    }

    public static void EnsureExchangeToQueueBinding(
        this RabbitMQMessagingTopology topology,
        string sourceExchangeName,
        string queueName)
    {
        topology.EnsureBinding(
            sourceExchangeName,
            queueName,
            RabbitMQDestinationKind.Queue,
            static (_, _, _) => new RabbitMQBindingConfiguration());
    }
}
