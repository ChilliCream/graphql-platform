using Mocha.Features;
using Mocha.Middlewares;
using static System.StringSplitOptions;

namespace Mocha.Transport.Postgres;

/// <summary>
/// Defines the endpoint and topology layout for the PostgreSQL transport.
/// </summary>
public sealed class PostgresRoutingStrategy : RoutingStrategy<PostgresMessagingTransport>
{
    private PostgresMessagingTopology _topology => field ??= (PostgresMessagingTopology)Transport.Topology;

    /// <inheritdoc />
    public override DispatchEndpointConfiguration? CreateEndpointConfiguration(
        IMessagingConfigurationContext context,
        OutboundRoute route)
    {
        if (route.Kind is not (OutboundRouteKind.Send or OutboundRouteKind.Publish))
        {
            return null;
        }

        var resolution = PostgresDestinations.Resolve(Transport.Schema, context.Naming, route);

        if (resolution.Kind == PostgresDestinationKind.Queue)
        {
            return new PostgresDispatchEndpointConfiguration
            {
                QueueName = resolution.Name,
                Name = resolution.EndpointName
            };
        }

        return new PostgresDispatchEndpointConfiguration
        {
            TopicName = resolution.Name,
            Name = resolution.EndpointName
        };
    }

    /// <inheritdoc />
    public override DispatchEndpointConfiguration? CreateEndpointConfiguration(
        IMessagingConfigurationContext context,
        Uri address)
    {
        PostgresDispatchEndpointConfiguration? configuration = null;

        var path = address.AbsolutePath.AsSpan();
        Span<Range> ranges = stackalloc Range[2];
        var segmentCount = path.Split(ranges, '/', RemoveEmptyEntries | TrimEntries);

        if (address.Scheme == Transport.Schema && address.Host is "")
        {
            if (segmentCount == 1 && path[ranges[0]] is "replies")
            {
                var instanceEndpointName = context.Naming.GetInstanceEndpoint(context.Host.InstanceId);
                configuration = new PostgresDispatchEndpointConfiguration
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

                if (kind is "t" && name is var topicName)
                {
                    configuration = new PostgresDispatchEndpointConfiguration
                    {
                        TopicName = new string(topicName),
                        Name = "t/" + new string(topicName)
                    };
                }

                if (kind is "q" && name is var queueName)
                {
                    configuration = new PostgresDispatchEndpointConfiguration
                    {
                        QueueName = new string(queueName),
                        Name = "q/" + new string(queueName)
                    };
                }
            }
        }

        if (configuration is null
            && TryParseTopologyAddress(address, out var topologyKind, out var resourceName))
        {
            if (topologyKind is 't')
            {
                configuration = new PostgresDispatchEndpointConfiguration
                {
                    TopicName = resourceName,
                    Name = "t/" + resourceName
                };
            }
            else if (topologyKind is 'q')
            {
                configuration = new PostgresDispatchEndpointConfiguration
                {
                    QueueName = resourceName,
                    Name = "q/" + resourceName
                };
            }
        }

        if (configuration is null && address is { Scheme: "queue" })
        {
            var name =
                !string.IsNullOrEmpty(address.Host) ? address.Host
                : segmentCount == 1 ? new string(path[ranges[0]]) : null;

            if (name is not null)
            {
                configuration = new PostgresDispatchEndpointConfiguration { QueueName = name, Name = "q/" + name };
            }
        }

        if (configuration is null && address is { Scheme: "topic" })
        {
            var name =
                !string.IsNullOrEmpty(address.Host) ? address.Host
                : segmentCount == 1 ? new string(path[ranges[0]]) : null;

            if (name is not null)
            {
                configuration = new PostgresDispatchEndpointConfiguration { TopicName = name, Name = "t/" + name };
            }
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
            return new PostgresReceiveEndpointConfiguration
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
        return new PostgresReceiveEndpointConfiguration { Name = queueName, QueueName = queueName };
    }

    public override void ConfigureEndpoint(
        IMessagingConfigurationContext context,
        ReceiveEndpointConfiguration configuration)
    {
        if (configuration is not PostgresReceiveEndpointConfiguration postgresConfiguration)
        {
            return;
        }

        postgresConfiguration.QueueName ??= postgresConfiguration.Name;

        if (postgresConfiguration is { Kind: ReceiveEndpointKind.Default, QueueName: { } queueName })
        {
            var faultFeature = postgresConfiguration.Features.GetOrSet<ReceiveFaultEndpointFeature>();
            ConfigureFaultOrSkippedEndpoint(
                context,
                queueName,
                ReceiveEndpointKind.Error,
                faultFeature,
                endpoint => faultFeature.Address ??= endpoint);

            var skippedFeature = postgresConfiguration.Features.GetOrSet<ReceiveSkippedEndpointFeature>();
            ConfigureFaultOrSkippedEndpoint(
                context,
                queueName,
                ReceiveEndpointKind.Skipped,
                skippedFeature,
                endpoint => skippedFeature.Address ??= endpoint);
        }

        if (Transport.Configuration is PostgresTransportConfiguration postgresConfigurationTransport)
        {
            postgresConfigurationTransport.Defaults.Endpoint.ApplyTo(postgresConfiguration);
        }
    }

    /// <inheritdoc />
    public override void DiscoverTopology(
        IMessagingConfigurationContext context,
        ReceiveEndpoint endpoint,
        ReceiveEndpointConfiguration configuration)
    {
        if (endpoint is not PostgresReceiveEndpoint postgresEndpoint
            || configuration is not PostgresReceiveEndpointConfiguration postgresConfiguration)
        {
            return;
        }

        if (postgresConfiguration.QueueName is null)
        {
            throw new InvalidOperationException("Queue name is required");
        }

        var queue = _topology.GetOrAddQueue(
            postgresConfiguration.QueueName,
            _ => new PostgresQueueConfiguration
            {
                AutoDelete = postgresConfiguration.IsTemporary,
                AutoProvision = postgresConfiguration.AutoProvision,
                Origin = TopologyOrigin.Endpoint
            });

        if (postgresConfiguration.IsTemporary && queue.AutoDelete == false)
        {
            throw new InvalidOperationException(
                $"Receive endpoint '{postgresConfiguration.Name}' is marked Temporary(), but its queue "
                    + $"'{postgresConfiguration.QueueName}' is explicitly configured with AutoDelete(false).");
        }

        if (postgresConfiguration.IsTemporary && queue.AutoDelete is null)
        {
            queue.MarkTemporary();
        }

        if (postgresEndpoint.Kind == ReceiveEndpointKind.Default)
        {
            EnsureFaultOrSkippedQueue(postgresConfiguration.Features.Get<ReceiveFaultEndpointFeature>()?.Address);
            EnsureFaultOrSkippedQueue(postgresConfiguration.Features.Get<ReceiveSkippedEndpointFeature>()?.Address);
        }

        if (postgresEndpoint.Kind
            is ReceiveEndpointKind.Reply
                or ReceiveEndpointKind.Error
                or ReceiveEndpointKind.Skipped)
        {
            return;
        }

        var schema = postgresEndpoint.Transport.Schema;
        var autoBind =
            (postgresConfiguration.BindMode ?? postgresEndpoint.Transport.BindMode) is MessagingBindMode.Implicit;

        var routes = context.Router.GetInboundByEndpoint(postgresEndpoint);
        foreach (var route in routes)
        {
            if (route.Kind is InboundRouteKind.Reply)
            {
                continue;
            }

            if (route.MessageType is not { } messageType)
            {
                continue;
            }

            var explicitPublishRoute = context
                .Router.GetOutboundByMessageType(messageType)
                .FirstOrDefault(r => r is { HasExplicitDestination: true, Kind: OutboundRouteKind.Publish });
            if (explicitPublishRoute is not null)
            {
                var destination = PostgresDestinations.Resolve(schema, context.Naming, explicitPublishRoute);

                if (destination.Kind == PostgresDestinationKind.Queue)
                {
                    continue;
                }

                EnsureTopic(_topology, destination.Name);
                if (autoBind)
                {
                    EnsureSubscription(_topology, destination.Name, postgresConfiguration.QueueName);
                }

                continue;
            }

            var publishTopicName = context.Naming.GetPublishEndpointName(messageType.RuntimeType);
            EnsureTopic(_topology, publishTopicName);

            var sendTopicName = context.Naming.GetSendEndpointName(messageType.RuntimeType);
            if (sendTopicName != publishTopicName)
            {
                EnsureTopic(_topology, sendTopicName);
                if (autoBind)
                {
                    EnsureSubscription(_topology, publishTopicName, postgresConfiguration.QueueName);
                }
            }

            if (autoBind)
            {
                EnsureSubscription(_topology, sendTopicName, postgresConfiguration.QueueName);
            }
        }
    }

    /// <inheritdoc />
    public override void DiscoverTopology(
        IMessagingConfigurationContext context,
        DispatchEndpoint endpoint,
        DispatchEndpointConfiguration configuration)
    {
        if (endpoint is not PostgresDispatchEndpoint
            || configuration is not PostgresDispatchEndpointConfiguration postgresConfiguration)
        {
            return;
        }

        if (postgresConfiguration.TopicName is not null
            && _topology.Topics.FirstOrDefault(t => t.Name == postgresConfiguration.TopicName) is null)
        {
            _topology.GetOrAddTopic(postgresConfiguration.TopicName, static _ => new PostgresTopicConfiguration());
        }

        if (postgresConfiguration.QueueName is not null
            && _topology.Queues.FirstOrDefault(q => q.Name == postgresConfiguration.QueueName) is null)
        {
            _topology.GetOrAddQueue(postgresConfiguration.QueueName, static _ => new PostgresQueueConfiguration());
        }

        if (postgresConfiguration.TopicName is not null
            && Transport.BindMode == MessagingBindMode.Implicit)
        {
            var schema = Transport.Schema;

            foreach (var (runtimeType, kind) in postgresConfiguration.Routes)
            {
                var messageType = context.Messages.GetMessageType(runtimeType);
                if (messageType is null)
                {
                    continue;
                }

                var outboundRoute = context
                    .Router.GetOutboundByMessageType(messageType)
                    .FirstOrDefault(r => r.Kind == kind);

                var destination = outboundRoute is not null
                    ? PostgresDestinations.Resolve(schema, context.Naming, outboundRoute)
                    : PostgresDestinations.ResolveConvention(context.Naming, kind, messageType);

                if (destination.Kind == PostgresDestinationKind.Queue)
                {
                    continue;
                }

                var topicName = destination.Name;

                if (postgresConfiguration.TopicName == topicName)
                {
                    continue;
                }

                if (_topology.Topics.FirstOrDefault(t => t.Name == topicName) is null)
                {
                    _topology.GetOrAddTopic(topicName, static _ => new PostgresTopicConfiguration());
                }
            }
        }
    }

    private static void EnsureTopic(PostgresMessagingTopology topology, string topicName)
    {
        if (topology.Topics.FirstOrDefault(e => e.Name == topicName) is null)
        {
            topology.GetOrAddTopic(topicName, static _ => new PostgresTopicConfiguration());
        }
    }

    private static void EnsureSubscription(PostgresMessagingTopology topology, string sourceTopicName, string queueName)
    {
        if (topology.Subscriptions.FirstOrDefault(s =>
                s.Source.Name == sourceTopicName && s.Destination.Name == queueName
            )
            is null)
        {
            topology.AddSubscription(
                new PostgresSubscriptionConfiguration { Source = sourceTopicName, Destination = queueName });
        }
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

    private void EnsureFaultOrSkippedQueue(Uri? address)
    {
        if (address is null || !TryGetQueueName(address, out var queueName))
        {
            return;
        }

        _topology.GetOrAddQueue(
            queueName,
            static _ => new PostgresQueueConfiguration { Origin = TopologyOrigin.Endpoint });
    }

    private bool TryGetQueueName(Uri address, out string queueName)
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

        if (TryParseTopologyAddress(address, out var topologyKind, out var topologyName) && topologyKind is 'q')
        {
            queueName = topologyName;
            return true;
        }

        if (address is { Scheme: "queue" })
        {
            queueName =
                !string.IsNullOrEmpty(address.Host) ? address.Host
                : segmentCount == 1 ? new string(path[ranges[0]]) : string.Empty;

            return queueName.Length > 0;
        }

        queueName = string.Empty;
        return false;
    }

    /// <summary>
    /// Parses an address under the transport topology into its kind and resource name.
    /// Everything after the kind segment is the name, because a queue or topic name may contain
    /// a slash.
    /// </summary>
    private bool TryParseTopologyAddress(Uri address, out char kind, out string name)
    {
        kind = default;
        name = string.Empty;

        // A valid resource path is not enough to identify this transport. For example,
        // postgres://other-host:5432/q/orders must not be claimed by this topology.
        var topologyAddress = Transport.Topology.Address;
        if (!address.Scheme.EqualsOrdinalIgnoreCase(topologyAddress.Scheme)
            || !address.Host.EqualsOrdinalIgnoreCase(topologyAddress.Host)
            || address.Port != topologyAddress.Port)
        {
            return false;
        }

        // Normalize only the topology path: "/" becomes empty and "/base/" becomes "/base".
        // Keep the address path unchanged while removing the topology prefix below.
        var basePath = topologyAddress.AbsolutePath.AsSpan().TrimEnd('/');
        var path = address.AbsolutePath.AsSpan();
        ReadOnlySpan<char> relativePath;

        if (basePath.IsEmpty)
        {
            // A root topology consumes exactly one structural slash:
            // "/q/orders" becomes "q/orders". The shorthand "postgres:q/orders" has no
            // leading slash and is handled by the transport-specific branch above.
            if (path.IsEmpty || path[0] is not '/')
            {
                return false;
            }

            relativePath = path[1..];
        }
        else
        {
            // Remove the exact topology base and its separator: "/base/q/orders" becomes
            // "q/orders". Requiring the separator prevents "/base" from matching
            // "/base-other/q/orders".
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
