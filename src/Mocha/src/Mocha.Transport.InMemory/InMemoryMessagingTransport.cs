using System.Diagnostics.CodeAnalysis;
using static System.StringSplitOptions;

namespace Mocha.Transport.InMemory;

/// <summary>
/// An in-memory messaging transport that routes messages through in-process topics and queues.
/// </summary>
/// <remarks>
/// Intended for development, testing, and single-process scenarios where no external broker is needed.
/// The topology (topics, queues, bindings) is built lazily during initialization and lives entirely
/// in the current application domain. Messages are never persisted to disk and are lost on process exit.
/// </remarks>
public sealed class InMemoryMessagingTransport : MessagingTransport
{
    private readonly Action<IInMemoryMessagingTransportDescriptor> _configure;

    /// <summary>
    /// Creates a new in-memory transport configured by the supplied delegate.
    /// </summary>
    /// <param name="configure">
    /// A delegate that receives an <see cref="IInMemoryMessagingTransportDescriptor"/> to declare
    /// endpoints, topology, middleware, and conventions for this transport.
    /// </param>
    public InMemoryMessagingTransport(Action<IInMemoryMessagingTransportDescriptor> configure)
    {
        _configure = configure;
    }

    private InMemoryMessagingTopology _topology = null!;

    /// <inheritdoc />
    public override MessagingTopology Topology => _topology;

    /// <summary>
    /// Builds the in-memory topology URI from the host's assembly name and creates the
    /// <see cref="InMemoryMessagingTopology"/> that holds all topics, queues, and bindings for
    /// this transport.
    /// </summary>
    /// <remarks>
    /// Called once during the messaging host initialization phase, after the base transport has
    /// been initialized. The topology address uses the transport schema and the assembly name as
    /// the host component, ensuring that endpoint addresses are scoped to this application.
    /// No network connections are established because all messaging is in-process.
    /// </remarks>
    /// <param name="context">The setup context providing access to the service provider and host configuration.</param>
    protected override void OnAfterInitialized(IMessagingSetupContext context)
    {
        var builder = new UriBuilder
        {
            Scheme = Schema,
            Host = context.Host.AssemblyName, // service name might be nicer
            Path = "/"
        };
        _topology = new InMemoryMessagingTopology(this, builder.Uri);

        var config = (InMemoryTransportConfiguration)Configuration;

        foreach (var topic in config.Topics)
        {
            _topology.AddTopic(topic);
        }

        foreach (var queue in config.Queues)
        {
            _topology.AddQueue(queue);
        }

        foreach (var binding in config.Bindings)
        {
            _topology.AddBinding(binding);
        }
    }

    /// <inheritdoc />
    public override TransportDescription Describe()
    {
        var receiveEndpoints = ReceiveEndpoints.Select(e => e.Describe()).ToList();

        var dispatchEndpoints = DispatchEndpoints.Select(e => e.Describe()).ToList();

        var entities = new List<TopologyEntityDescription>();
        var links = new List<TopologyLinkDescription>();

        foreach (var topic in _topology.Topics)
        {
            entities.Add(
                new TopologyEntityDescription("topic", topic.Name, topic.Address?.ToString(), "inbound", null));
        }

        foreach (var queue in _topology.Queues)
        {
            entities.Add(
                new TopologyEntityDescription("queue", queue.Name, queue.Address?.ToString(), "outbound", null));
        }

        foreach (var binding in _topology.Bindings)
        {
            links.Add(
                new TopologyLinkDescription(
                    "bind",
                    binding.Address?.ToString(),
                    binding.Source.Address?.ToString(),
                    binding switch
                    {
                        InMemoryQueueBinding qb => qb.Destination.Address?.ToString(),
                        InMemoryTopicBinding tb => tb.Destination.Address?.ToString(),
                        _ => null
                    },
                    "forward",
                    null));
        }

        var topology = new TopologyDescription(_topology.Address.ToString(), entities, links);

        return new TransportDescription(
            _topology.Address.ToString(),
            Name,
            Schema,
            GetType().Name,
            receiveEndpoints,
            dispatchEndpoints,
            topology);
    }

    /// <inheritdoc />
    public override bool TryGetDispatchEndpoint(Uri address, [NotNullWhen(true)] out DispatchEndpoint? endpoint)
    {
        if (address.Scheme == Schema)
        {
            foreach (var candidate in DispatchEndpoints)
            {
                if (candidate.Address == address)
                {
                    endpoint = candidate;
                    return true;
                }
            }
        }

        if (Topology.Address.IsBaseOf(address))
        {
            foreach (var candidate in DispatchEndpoints)
            {
                if (candidate.Destination.Address == address)
                {
                    endpoint = candidate;
                    return true;
                }
            }
        }

        if (address is { Scheme: "queue", Host: { Length: > 0 } queueName })
        {
            foreach (var candidate in DispatchEndpoints)
            {
                if (candidate.Destination is InMemoryQueue queue && queue.Name == queueName)
                {
                    endpoint = candidate;
                    return true;
                }
            }
        }

        if (address is { Scheme: "topic", Host: { Length: > 0 } topicName })
        {
            foreach (var candidate in DispatchEndpoints)
            {
                if (candidate.Destination is InMemoryTopic topic && topic.Name == topicName)
                {
                    endpoint = candidate;
                    return true;
                }
            }
        }

        endpoint = null;
        return false;
    }

    /// <summary>
    /// Builds the in-memory transport configuration by invoking the user-supplied configuration
    /// delegate on an <see cref="InMemoryMessagingTransportDescriptor"/>.
    /// </summary>
    /// <remarks>
    /// The descriptor collects endpoint definitions, topology declarations, middleware, and
    /// conventions, then produces a <see cref="MessagingTransportConfiguration"/> that the base
    /// class uses to wire up receive and dispatch pipelines. No broker-specific settings are
    /// needed since all messaging happens in-process.
    /// </remarks>
    /// <param name="context">The setup context providing access to the service provider and host configuration.</param>
    /// <returns>A <see cref="MessagingTransportConfiguration"/> containing all in-memory endpoint and pipeline definitions.</returns>
    protected override MessagingTransportConfiguration CreateConfiguration(IMessagingSetupContext context)
    {
        var descriptor = new InMemoryMessagingTransportDescriptor(context);

        _configure(descriptor);

        return descriptor.CreateConfiguration();
    }

    /// <summary>
    /// Creates a new <see cref="InMemoryReceiveEndpoint"/> bound to this transport, which will
    /// receive messages from an in-memory queue without any network I/O.
    /// </summary>
    /// <returns>A new, uninitialized <see cref="InMemoryReceiveEndpoint"/> for this transport.</returns>
    protected override ReceiveEndpoint CreateReceiveEndpoint()
    {
        return new InMemoryReceiveEndpoint(this);
    }

    /// <summary>
    /// Creates a new <see cref="InMemoryDispatchEndpoint"/> bound to this transport, which will
    /// dispatch messages to in-memory topics or queues without any network I/O.
    /// </summary>
    /// <returns>A new, uninitialized <see cref="InMemoryDispatchEndpoint"/> for this transport.</returns>
    protected override DispatchEndpoint CreateDispatchEndpoint()
    {
        return new InMemoryDispatchEndpoint(this);
    }

    /// <inheritdoc />
    public override DispatchEndpointConfiguration? CreateEndpointConfiguration(
        IMessagingConfigurationContext context,
        OutboundRoute route)
    {
        InMemoryDispatchEndpointConfiguration? configuration = null;
        if (route.Kind == OutboundRouteKind.Send)
        {
            var queueName = context.Naming.GetSendEndpointName(route.MessageType.RuntimeType);
            configuration = new InMemoryDispatchEndpointConfiguration
            {
                QueueName = queueName,
                Name = "q/" + queueName
            };
        }
        else if (route.Kind == OutboundRouteKind.Publish)
        {
            var topicName = context.Naming.GetPublishEndpointName(route.MessageType.RuntimeType);
            configuration = new InMemoryDispatchEndpointConfiguration
            {
                TopicName = topicName,
                Name = "t/" + topicName
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

        if (address.Scheme == Schema && address.Host is "")
        {
            if (segmentCount == 1 && path[ranges[0]] is "replies")
            {
                var instanceEndpointName = context.Naming.GetInstanceEndpoint(context.Host.InstanceId);
                configuration = new InMemoryDispatchEndpointConfiguration
                {
                    Kind = DispatchEndpointKind.Reply,
                    // TODO the idea of the reply endpoint is to be able to dispatch to ANY queue.
                    // so this is technically not correct but it's the easiest way to make the endpoint
                    // complete
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

        if (configuration is null && _topology.Address.IsBaseOf(address) && segmentCount == 2)
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

        if (configuration is null && address is { Scheme: "queue" })
        {
            var name =
                !string.IsNullOrEmpty(address.Host) ? address.Host
                : segmentCount == 1 ? new string(path[ranges[0]]) : null;

            if (name is not null)
            {
                configuration = new InMemoryDispatchEndpointConfiguration { QueueName = name, Name = "q/" + name };
            }
        }

        if (configuration is null && address is { Scheme: "topic" })
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
}
