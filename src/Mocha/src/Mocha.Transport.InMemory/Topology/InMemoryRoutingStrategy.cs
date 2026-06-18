using Mocha.Middlewares;
using static System.StringSplitOptions;

namespace Mocha.Transport.InMemory;

/// <summary>
/// Defines the endpoint and topology layout for the in-memory transport.
/// </summary>
public sealed class InMemoryRoutingStrategy : RoutingStrategy<InMemoryMessagingTransport>
{
    private InMemoryMessagingTopology _topology =>
        field ??= (InMemoryMessagingTopology)Transport.Topology;

    /// <inheritdoc />
    public override DispatchEndpointConfiguration? CreateEndpointConfiguration(
        IMessagingConfigurationContext context,
        OutboundRoute route)
    {
        if (route.Kind is not (OutboundRouteKind.Send or OutboundRouteKind.Publish))
        {
            return null;
        }

        var resolution = InMemoryDestinations.Resolve(Transport.Schema, context.Naming, route);

        if (resolution.Kind == InMemoryDestinationKind.Queue)
        {
            return new InMemoryDispatchEndpointConfiguration
            {
                QueueName = resolution.Name,
                Name = resolution.EndpointName
            };
        }

        return new InMemoryDispatchEndpointConfiguration
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
        InMemoryDispatchEndpointConfiguration? configuration = null;

        var path = address.AbsolutePath.AsSpan();
        Span<Range> ranges = stackalloc Range[2];
        var segmentCount = path.Split(ranges, '/', RemoveEmptyEntries | TrimEntries);

        if (address.Scheme == Transport.Schema && address.Host is "")
        {
            if (segmentCount == 1 && path[ranges[0]] is "replies")
            {
                var instanceEndpointName = context.Naming.GetInstanceEndpoint(context.Host.InstanceId);
                configuration = new InMemoryDispatchEndpointConfiguration
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
                    configuration = new InMemoryDispatchEndpointConfiguration
                    {
                        TopicName = new string(topicName),
                        Name = "t/" + new string(topicName)
                    };
                }

                if (kind is "q" && name is var queueName)
                {
                    configuration = new InMemoryDispatchEndpointConfiguration
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
                configuration = new InMemoryDispatchEndpointConfiguration
                {
                    TopicName = new string(topicName),
                    Name = "t/" + new string(topicName)
                };
            }

            if (kind is "q" && name is var queueName)
            {
                configuration = new InMemoryDispatchEndpointConfiguration
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
                configuration = new InMemoryDispatchEndpointConfiguration { QueueName = name, Name = "q/" + name };
            }
        }

        if (configuration is null && isEffectiveDefault && address is { Scheme: "topic" })
        {
            var name =
                !string.IsNullOrEmpty(address.Host) ? address.Host
                : segmentCount == 1 ? new string(path[ranges[0]]) : null;

            if (name is not null)
            {
                configuration = new InMemoryDispatchEndpointConfiguration { TopicName = name, Name = "t/" + name };
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
            return new InMemoryReceiveEndpointConfiguration
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
        return new InMemoryReceiveEndpointConfiguration { Name = queueName, QueueName = queueName };
    }

    public override void ConfigureEndpoint(
        IMessagingConfigurationContext context,
        ReceiveEndpointConfiguration configuration)
    {
        if (configuration is InMemoryReceiveEndpointConfiguration inMemoryConfiguration)
        {
            inMemoryConfiguration.QueueName ??= inMemoryConfiguration.Name;
        }
    }

    /// <inheritdoc />
    public override void DiscoverTopology(
        IMessagingConfigurationContext context,
        ReceiveEndpoint endpoint,
        ReceiveEndpointConfiguration configuration)
    {
        if (endpoint is not InMemoryReceiveEndpoint inMemoryEndpoint
            || configuration is not InMemoryReceiveEndpointConfiguration inMemoryConfiguration)
        {
            return;
        }

        if (inMemoryConfiguration.QueueName is null)
        {
            throw new InvalidOperationException("Queue name is required");
        }

        if (_topology.Queues.FirstOrDefault(q => q.Name == inMemoryConfiguration.QueueName) is null)
        {
            _topology.AddQueue(new InMemoryQueueConfiguration { Name = inMemoryConfiguration.QueueName });
        }

        if (inMemoryEndpoint.Kind is ReceiveEndpointKind.Reply or ReceiveEndpointKind.Error or ReceiveEndpointKind.Skipped)
        {
            return;
        }

        var schema = inMemoryEndpoint.Transport.Schema;
        var autoBind = (inMemoryConfiguration.BindMode ?? inMemoryEndpoint.Transport.BindMode)
            is MessagingBindMode.Implicit;

        var routes = context.Router.GetInboundByEndpoint(inMemoryEndpoint);
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

            if (!autoBind)
            {
                continue;
            }

            var explicitPublishRoute = context.Router.GetOutboundByMessageType(route.MessageType)
                .FirstOrDefault(r => r is { HasExplicitDestination: true, Kind: OutboundRouteKind.Publish });
            if (explicitPublishRoute is not null)
            {
                var destination = InMemoryDestinations.Resolve(schema, context.Naming, explicitPublishRoute);

                if (destination.Kind == InMemoryDestinationKind.Queue)
                {
                    continue;
                }

                EnsureTopic(_topology, destination.Name);
                EnsureQueueBinding(_topology, destination.Name, inMemoryConfiguration.QueueName);

                continue;
            }

            var publishTopicName = context.Naming.GetPublishEndpointName(route.MessageType.RuntimeType);
            EnsureTopic(_topology, publishTopicName);

            var sendTopicName = context.Naming.GetSendEndpointName(route.MessageType.RuntimeType);
            if (sendTopicName != publishTopicName)
            {
                EnsureTopic(_topology, sendTopicName);
                EnsureTopicBinding(_topology, publishTopicName, sendTopicName);
            }

            EnsureQueueBinding(_topology, sendTopicName, inMemoryConfiguration.QueueName);
        }
    }

    /// <inheritdoc />
    public override void DiscoverTopology(
        IMessagingConfigurationContext context,
        DispatchEndpoint endpoint,
        DispatchEndpointConfiguration configuration)
    {
        if (endpoint is not InMemoryDispatchEndpoint
            || configuration is not InMemoryDispatchEndpointConfiguration inMemoryConfiguration)
        {
            return;
        }

        if (inMemoryConfiguration.TopicName is not null
            && _topology.Topics.FirstOrDefault(t => t.Name == inMemoryConfiguration.TopicName) is null)
        {
            _topology.AddTopic(new InMemoryTopicConfiguration { Name = inMemoryConfiguration.TopicName });
        }

        if (inMemoryConfiguration.QueueName is not null
            && _topology.Queues.FirstOrDefault(q => q.Name == inMemoryConfiguration.QueueName) is null)
        {
            _topology.AddQueue(new InMemoryQueueConfiguration { Name = inMemoryConfiguration.QueueName });
        }
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
}
