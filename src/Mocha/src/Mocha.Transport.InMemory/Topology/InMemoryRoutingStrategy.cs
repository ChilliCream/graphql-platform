using Mocha.Middlewares;
using static System.StringSplitOptions;

namespace Mocha.Transport.InMemory;

/// <summary>
/// Defines the endpoint and topology layout for the in-memory transport.
/// </summary>
public sealed class InMemoryRoutingStrategy(InMemoryMessagingTransport transport)
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

        InMemoryDispatchEndpointConfiguration configuration;
        if (resolution.Kind == InMemoryDestinationKind.Queue)
        {
            configuration = new InMemoryDispatchEndpointConfiguration
            {
                QueueName = resolution.Name,
                Name = resolution.EndpointName
            };
        }
        else
        {
            configuration = new InMemoryDispatchEndpointConfiguration
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
        InMemoryDispatchEndpointConfiguration? configuration = null;

        var path = address.AbsolutePath.AsSpan();
        Span<Range> ranges = stackalloc Range[2];
        var segmentCount = path.Split(ranges, '/', RemoveEmptyEntries | TrimEntries);

        if (address.Scheme == transport.Schema && address.Host is "")
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

        if (configuration is null && transport.Topology.Address.IsBaseOf(address) && segmentCount == 2)
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

        var isEffectiveDefault = transport.IsDefaultTransport || context.Transports.Length == 1;

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
        InMemoryReceiveEndpointConfiguration configuration;
        if (route.Kind == InboundRouteKind.Reply)
        {
            var instanceEndpointName = context.Naming.GetInstanceEndpoint(context.Host.InstanceId);
            configuration = new InMemoryReceiveEndpointConfiguration
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
            configuration = new InMemoryReceiveEndpointConfiguration { Name = queueName, QueueName = queueName };
        }

        return configuration;
    }

    /// <inheritdoc />
    public override void DiscoverTopology(
        IMessagingConfigurationContext context,
        ReceiveEndpoint endpoint,
        ReceiveEndpointConfiguration configuration)
    {
        if (endpoint is InMemoryReceiveEndpoint inMemoryEndpoint
            && configuration is InMemoryReceiveEndpointConfiguration inMemoryConfiguration)
        {
            DiscoverReceiveTopology(context, inMemoryEndpoint, inMemoryConfiguration);
        }
    }

    /// <inheritdoc />
    public override void DiscoverTopology(
        IMessagingConfigurationContext context,
        DispatchEndpoint endpoint,
        DispatchEndpointConfiguration configuration)
    {
        if (endpoint is InMemoryDispatchEndpoint inMemoryEndpoint
            && configuration is InMemoryDispatchEndpointConfiguration inMemoryConfiguration)
        {
            DiscoverDispatchTopology(inMemoryEndpoint, inMemoryConfiguration);
        }
    }

    internal static void DiscoverReceiveTopology(
        IMessagingConfigurationContext context,
        InMemoryReceiveEndpoint endpoint,
        InMemoryReceiveEndpointConfiguration configuration)
    {
        if (configuration.QueueName is null)
        {
            throw new InvalidOperationException("Queue name is required");
        }

        var topology = (InMemoryMessagingTopology)endpoint.Transport.Topology;

        if (topology.Queues.FirstOrDefault(q => q.Name == configuration.QueueName) is null)
        {
            topology.AddQueue(new InMemoryQueueConfiguration { Name = configuration.QueueName });
        }

        if (endpoint.Kind is ReceiveEndpointKind.Reply or ReceiveEndpointKind.Error or ReceiveEndpointKind.Skipped)
        {
            return;
        }

        var resolver = ((InMemoryMessagingTransport)endpoint.Transport).Resolver;

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

            if (chainEntry.Kind == InMemoryDestinationKind.Queue)
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
                    EnsureQueueBinding(topology, chainEntry.Name, configuration.QueueName);
                }

                continue;
            }

            var publishTopicName = chainEntry.Name;
            EnsureTopic(topology, publishTopicName);

            var sendTopicName = context.Naming.GetSendEndpointName(route.MessageType.RuntimeType);
            if (sendTopicName != publishTopicName)
            {
                EnsureTopic(topology, sendTopicName);
                EnsureTopicBinding(topology, publishTopicName, sendTopicName);
            }

            if (autoBind)
            {
                EnsureQueueBinding(topology, sendTopicName, configuration.QueueName);
            }
        }
    }

    internal static void DiscoverDispatchTopology(
        InMemoryDispatchEndpoint endpoint,
        InMemoryDispatchEndpointConfiguration configuration)
    {
        var topology = (InMemoryMessagingTopology)endpoint.Transport.Topology;

        if (configuration.TopicName is not null
            && topology.Topics.FirstOrDefault(t => t.Name == configuration.TopicName) is null)
        {
            topology.AddTopic(new InMemoryTopicConfiguration { Name = configuration.TopicName });
        }

        if (configuration.QueueName is not null
            && topology.Queues.FirstOrDefault(q => q.Name == configuration.QueueName) is null)
        {
            topology.AddQueue(new InMemoryQueueConfiguration { Name = configuration.QueueName });
        }
    }

    private static bool ResolveAutoBind(
        MessagingTransport transport,
        InMemoryReceiveEndpointConfiguration configuration)
    {
        if (configuration.BindMode.HasValue)
        {
            return configuration.BindMode.Value == MessagingBindMode.Implicit;
        }

        return transport.BindMode == MessagingBindMode.Implicit;
    }

    private static ChainEntry ResolveChainEntry(
        IMessagingConfigurationContext context,
        InMemoryDestinationResolver resolver,
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

    private static string GetTypeName(MessageType messageType)
        => messageType.RuntimeType.FullName ?? messageType.RuntimeType.Name;

    private readonly record struct ChainEntry(InMemoryDestinationKind Kind, string Name, bool IsExplicit);
}
