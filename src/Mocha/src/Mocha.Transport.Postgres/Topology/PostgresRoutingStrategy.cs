using Mocha.Middlewares;
using static System.StringSplitOptions;

namespace Mocha.Transport.Postgres;

/// <summary>
/// Defines the endpoint and topology layout for the PostgreSQL transport.
/// </summary>
public sealed class PostgresRoutingStrategy : RoutingStrategy
{
    private PostgresMessagingTransport PostgresTransport => (PostgresMessagingTransport)Transport;

    /// <inheritdoc />
    public override DispatchEndpointConfiguration? CreateEndpointConfiguration(
        IMessagingConfigurationContext context,
        OutboundRoute route)
    {
        if (route.Kind is not (OutboundRouteKind.Send or OutboundRouteKind.Publish))
        {
            return null;
        }

        var resolution = PostgresTransport.Resolver.ResolveDestination(context.Naming, route);

        PostgresDispatchEndpointConfiguration configuration;
        if (resolution.Kind == PostgresDestinationKind.Queue)
        {
            configuration = new PostgresDispatchEndpointConfiguration
            {
                QueueName = resolution.Name,
                Name = resolution.EndpointName
            };
        }
        else
        {
            configuration = new PostgresDispatchEndpointConfiguration
            {
                TopicName = resolution.Name,
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
        PostgresDispatchEndpointConfiguration? configuration = null;

        var path = address.AbsolutePath.AsSpan();
        Span<Range> ranges = stackalloc Range[2];
        var segmentCount = path.Split(ranges, '/', RemoveEmptyEntries | TrimEntries);

        if (address.Scheme == PostgresTransport.Schema && address.Host is "")
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

        if (configuration is null && PostgresTransport.Topology.Address.IsBaseOf(address) && segmentCount == 2)
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

        var isEffectiveDefault = PostgresTransport.IsDefaultTransport || context.Transports.Length == 1;

        if (configuration is null && isEffectiveDefault && address is { Scheme: "queue" })
        {
            var name =
                !string.IsNullOrEmpty(address.Host) ? address.Host
                : segmentCount == 1 ? new string(path[ranges[0]]) : null;

            if (name is not null)
            {
                configuration = new PostgresDispatchEndpointConfiguration { QueueName = name, Name = "q/" + name };
            }
        }

        if (configuration is null && isEffectiveDefault && address is { Scheme: "topic" })
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
        PostgresReceiveEndpointConfiguration configuration;
        if (route.Kind == InboundRouteKind.Reply)
        {
            var instanceEndpointName = context.Naming.GetInstanceEndpoint(context.Host.InstanceId);
            configuration = new PostgresReceiveEndpointConfiguration
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
            configuration = new PostgresReceiveEndpointConfiguration { Name = queueName, QueueName = queueName };
        }

        return configuration;
    }

    /// <inheritdoc />
    public override void DiscoverTopology(
        IMessagingConfigurationContext context,
        ReceiveEndpoint endpoint,
        ReceiveEndpointConfiguration configuration)
    {
        if (endpoint is PostgresReceiveEndpoint postgresEndpoint
            && configuration is PostgresReceiveEndpointConfiguration postgresConfiguration)
        {
            DiscoverReceiveTopology(context, postgresEndpoint, postgresConfiguration);
        }
    }

    /// <inheritdoc />
    public override void DiscoverTopology(
        IMessagingConfigurationContext context,
        DispatchEndpoint endpoint,
        DispatchEndpointConfiguration configuration)
    {
        if (endpoint is PostgresDispatchEndpoint postgresEndpoint
            && configuration is PostgresDispatchEndpointConfiguration postgresConfiguration)
        {
            DiscoverDispatchTopology(context, postgresEndpoint, postgresConfiguration);
        }
    }

    internal static void DiscoverReceiveTopology(
        IMessagingConfigurationContext context,
        PostgresReceiveEndpoint endpoint,
        PostgresReceiveEndpointConfiguration configuration)
    {
        if (configuration.QueueName is null)
        {
            throw new InvalidOperationException("Queue name is required");
        }

        var topology = (PostgresMessagingTopology)endpoint.Transport.Topology;

        topology.AddQueue(
            new PostgresQueueConfiguration
            {
                Name = configuration.QueueName,
                AutoDelete = endpoint.Kind == ReceiveEndpointKind.Reply,
                AutoProvision = configuration.AutoProvision
            });

        if (endpoint.Kind is ReceiveEndpointKind.Reply or ReceiveEndpointKind.Error or ReceiveEndpointKind.Skipped)
        {
            return;
        }

        var resolver = ((PostgresMessagingTransport)endpoint.Transport).Resolver;

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
            var chainEntry = ResolveChainEntry(context, resolver, route.MessageType);

            if (chainEntry.Kind == PostgresDestinationKind.Queue)
            {
                if (autoBind)
                {
                    throw ThrowHelper.ConsumeBindUnderivable(GetTypeName(route.MessageType), configuration.QueueName);
                }

                continue;
            }

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

            var sendTopicName = context.Naming.GetSendEndpointName(route.MessageType.RuntimeType);
            if (sendTopicName != publishTopicName)
            {
                EnsureTopic(topology, sendTopicName);

                if (autoBind)
                {
                    EnsureSubscription(topology, publishTopicName, configuration.QueueName);
                }
            }

            if (autoBind)
            {
                EnsureSubscription(topology, sendTopicName, configuration.QueueName);
            }
        }
    }

    internal static void DiscoverDispatchTopology(
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

                var outboundRoute = context.Router.GetOutboundByMessageType(messageType)
                    .FirstOrDefault(r => r.Kind == kind);

                PostgresDestinationResolution chainEntry;
                if (outboundRoute is not null)
                {
                    chainEntry = resolver.ResolveDestination(context.Naming, outboundRoute);
                }
                else
                {
                    chainEntry = kind == OutboundRouteKind.Publish
                        ? resolver.ResolvePublishDestination(context.Naming, messageType)
                        : new PostgresDestinationResolution(
                            PostgresDestinationKind.Topic,
                            context.Naming.GetSendEndpointName(runtimeType),
                            "t/" + context.Naming.GetSendEndpointName(runtimeType));
                }

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

    private static bool ResolveAutoBind(
        MessagingTransport transport,
        PostgresReceiveEndpointConfiguration configuration)
    {
        if (configuration.BindMode.HasValue)
        {
            return configuration.BindMode.Value == MessagingBindMode.Implicit;
        }

        return transport.BindMode == MessagingBindMode.Implicit;
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
