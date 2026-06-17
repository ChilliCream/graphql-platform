using Mocha.Middlewares;
using static System.StringSplitOptions;

namespace Mocha.Transport.Postgres;

/// <summary>
/// Defines the endpoint and topology layout for the PostgreSQL transport.
/// </summary>
public sealed class PostgresRoutingStrategy : RoutingStrategy<PostgresMessagingTransport>
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

        if (configuration is null && Transport.Topology.Address.IsBaseOf(address) && segmentCount == 2)
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

        var isEffectiveDefault = Transport.IsDefaultTransport || context.Transports.Length == 1;

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

        var topology = (PostgresMessagingTopology)postgresEndpoint.Transport.Topology;

        topology.AddQueue(
            new PostgresQueueConfiguration
            {
                Name = postgresConfiguration.QueueName,
                AutoDelete = postgresEndpoint.Kind == ReceiveEndpointKind.Reply,
                AutoProvision = postgresConfiguration.AutoProvision
            });

        if (postgresEndpoint.Kind is ReceiveEndpointKind.Reply or ReceiveEndpointKind.Error or ReceiveEndpointKind.Skipped)
        {
            return;
        }

        var schema = postgresEndpoint.Transport.Schema;

        var routes = context.Router.GetInboundByEndpoint(postgresEndpoint);
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

            var autoBind = (postgresConfiguration.BindMode ?? postgresEndpoint.Transport.BindMode)
                is MessagingBindMode.Implicit;
            var explicitPublishRoute = context.Router.GetOutboundByMessageType(route.MessageType)
                .FirstOrDefault(r => r is { HasExplicitDestination: true, Kind: OutboundRouteKind.Publish });
            if (explicitPublishRoute is not null)
            {
                var destination = PostgresDestinations.Resolve(schema, context.Naming, explicitPublishRoute);

                if (destination.Kind == PostgresDestinationKind.Queue)
                {
                    if (autoBind)
                    {
                        throw ThrowHelper.ConsumeBindUnderivable(GetTypeName(route.MessageType), postgresConfiguration.QueueName);
                    }

                    continue;
                }

                EnsureTopic(topology, destination.Name);

                if (autoBind)
                {
                    EnsureSubscription(topology, destination.Name, postgresConfiguration.QueueName);
                }

                continue;
            }

            var publishTopicName = context.Naming.GetPublishEndpointName(route.MessageType.RuntimeType);
            EnsureTopic(topology, publishTopicName);

            var sendTopicName = context.Naming.GetSendEndpointName(route.MessageType.RuntimeType);
            if (sendTopicName != publishTopicName)
            {
                EnsureTopic(topology, sendTopicName);

                if (autoBind)
                {
                    EnsureSubscription(topology, publishTopicName, postgresConfiguration.QueueName);
                }
            }

            if (autoBind)
            {
                EnsureSubscription(topology, sendTopicName, postgresConfiguration.QueueName);
            }
        }
    }

    /// <inheritdoc />
    public override void DiscoverTopology(
        IMessagingConfigurationContext context,
        DispatchEndpoint endpoint,
        DispatchEndpointConfiguration configuration)
    {
        if (endpoint is not PostgresDispatchEndpoint postgresEndpoint
            || configuration is not PostgresDispatchEndpointConfiguration postgresConfiguration)
        {
            return;
        }

        var topology = (PostgresMessagingTopology)postgresEndpoint.Transport.Topology;

        if (postgresConfiguration.TopicName is not null
            && topology.Topics.FirstOrDefault(t => t.Name == postgresConfiguration.TopicName) is null)
        {
            topology.AddTopic(new PostgresTopicConfiguration { Name = postgresConfiguration.TopicName });
        }

        if (postgresConfiguration.QueueName is not null
            && topology.Queues.FirstOrDefault(q => q.Name == postgresConfiguration.QueueName) is null)
        {
            topology.AddQueue(new PostgresQueueConfiguration { Name = postgresConfiguration.QueueName });
        }

        if (postgresConfiguration.TopicName is not null
            && postgresEndpoint.Transport.BindMode == MessagingBindMode.Implicit)
        {
            var schema = postgresEndpoint.Transport.Schema;

            foreach (var (runtimeType, kind) in postgresConfiguration.Routes)
            {
                var messageType = context.Messages.GetMessageType(runtimeType);
                if (messageType is null)
                {
                    continue;
                }

                var outboundRoute = context.Router.GetOutboundByMessageType(messageType)
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

                if (topology.Topics.FirstOrDefault(t => t.Name == topicName) is null)
                {
                    topology.AddTopic(new PostgresTopicConfiguration { Name = topicName });
                }
            }
        }
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
}
