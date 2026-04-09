using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha.Transport.Kafka.Connection;
using static System.StringSplitOptions;

namespace Mocha.Transport.Kafka;

/// <summary>
/// Kafka implementation of <see cref="MessagingTransport"/> that manages connections, topology provisioning,
/// and the lifecycle of receive and dispatch endpoints backed by Kafka topics and consumer groups.
/// </summary>
public sealed class KafkaMessagingTransport : MessagingTransport
{
    private readonly Action<IKafkaMessagingTransportDescriptor> _configure;
    private KafkaMessagingTopology _topology = null!;

    /// <summary>
    /// Creates a new Kafka transport with the specified configuration delegate.
    /// </summary>
    /// <param name="configure">A delegate that configures the transport descriptor with endpoints, topology, and connection settings.</param>
    public KafkaMessagingTransport(Action<IKafkaMessagingTransportDescriptor> configure)
    {
        _configure = configure;
    }

    /// <inheritdoc />
    public override MessagingTopology Topology => _topology;

    /// <summary>
    /// Gets the connection manager responsible for producer, consumer, and admin client lifecycle.
    /// </summary>
    public KafkaConnectionManager ConnectionManager { get; private set; } = null!;

    /// <summary>
    /// Resolves the Kafka bootstrap servers, builds the transport topology URI, and creates
    /// the <see cref="ConnectionManager"/> instance used for the lifetime of this transport.
    /// </summary>
    /// <param name="context">The setup context providing access to the service provider and host configuration.</param>
    protected override void OnAfterInitialized(IMessagingSetupContext context)
    {
        var configuration = (KafkaTransportConfiguration)Configuration;

        var bootstrapServers = configuration.BootstrapServers
            ?? throw new InvalidOperationException("BootstrapServers is required");

        // Build topology URI from first bootstrap server
        var firstServer = bootstrapServers.Split(',')[0].Trim();
        var parts = firstServer.Split(':');
        var builder = new UriBuilder
        {
            Scheme = Schema,
            Host = parts[0],
            Port = parts.Length > 1 && int.TryParse(parts[1], out var port) ? port : 9092,
            Path = "/"
        };

        _topology = new KafkaMessagingTopology(
            this,
            builder.Uri,
            configuration.Defaults,
            configuration.AutoProvision ?? true);

        foreach (var topic in configuration.Topics)
        {
            _topology.AddTopic(topic);
        }

        var logger = context.Services.GetRequiredService<ILogger<KafkaConnectionManager>>();
        ConnectionManager = new KafkaConnectionManager(
            logger,
            bootstrapServers,
            configuration.ProducerConfigOverrides,
            configuration.ConsumerConfigOverrides);
    }

    /// <summary>
    /// Establishes the producer connection and optionally provisions topology.
    /// </summary>
    /// <param name="context">The configuration context for the current startup phase.</param>
    /// <param name="cancellationToken">A token to cancel the connection establishment.</param>
    protected override async ValueTask OnBeforeStartAsync(
        IMessagingConfigurationContext context,
        CancellationToken cancellationToken)
    {
        ConnectionManager.EnsureProducerCreated();

        if (_topology.AutoProvision)
        {
            await ConnectionManager.ProvisionTopologyAsync(
                _topology.Topics.Where(t => t.AutoProvision ?? true),
                cancellationToken);
        }
    }

    /// <inheritdoc />
    protected override MessagingTransportConfiguration CreateConfiguration(IMessagingSetupContext context)
    {
        var descriptor = new KafkaMessagingTransportDescriptor(context);
        _configure(descriptor);
        return descriptor.CreateConfiguration();
    }

    /// <inheritdoc />
    protected override ReceiveEndpoint CreateReceiveEndpoint()
    {
        return new KafkaReceiveEndpoint(this);
    }

    /// <inheritdoc />
    protected override DispatchEndpoint CreateDispatchEndpoint()
    {
        return new KafkaDispatchEndpoint(this);
    }

    /// <inheritdoc />
    public override DispatchEndpointConfiguration? CreateEndpointConfiguration(
        IMessagingConfigurationContext context,
        OutboundRoute route)
    {
        // Both publish and send map to a topic.
        // Publish: topic named after message type (fanout -- each consumer group gets all messages).
        // Send: same topic, but consumers use the same consumer group (competing consumers).
        var topicName = route.Kind == OutboundRouteKind.Publish
            ? context.Naming.GetPublishEndpointName(route.MessageType.RuntimeType)
            : context.Naming.GetSendEndpointName(route.MessageType.RuntimeType);

        return new KafkaDispatchEndpointConfiguration
        {
            TopicName = topicName,
            Name = "t/" + topicName
        };
    }

    /// <inheritdoc />
    public override DispatchEndpointConfiguration? CreateEndpointConfiguration(
        IMessagingConfigurationContext context,
        Uri address)
    {
        KafkaDispatchEndpointConfiguration? configuration = null;

        var path = address.AbsolutePath.AsSpan();
        Span<Range> ranges = stackalloc Range[2];
        var segmentCount = path.Split(ranges, '/', RemoveEmptyEntries | TrimEntries);

        // kafka:///replies -> reply endpoint
        if (address.Scheme == Schema && address.Host is "")
        {
            if (segmentCount == 1 && path[ranges[0]] is "replies")
            {
                var instanceTopicName = context.Naming.GetInstanceEndpoint(context.Host.InstanceId);
                configuration = new KafkaDispatchEndpointConfiguration
                {
                    Kind = DispatchEndpointKind.Reply,
                    TopicName = instanceTopicName,
                    Name = "Replies"
                };
            }

            // kafka:///t/topic_name -> topic dispatch
            if (segmentCount == 2)
            {
                var kind = path[ranges[0]];
                var name = path[ranges[1]];

                if (kind is "t")
                {
                    configuration = new KafkaDispatchEndpointConfiguration
                    {
                        TopicName = new string(name),
                        Name = "t/" + new string(name)
                    };
                }
            }
        }

        // kafka://host:port/t/topic_name -> fully qualified topic dispatch
        if (configuration is null && _topology.Address.IsBaseOf(address) && segmentCount == 2)
        {
            var kind = path[ranges[0]];
            var name = path[ranges[1]];

            if (kind is "t")
            {
                configuration = new KafkaDispatchEndpointConfiguration
                {
                    TopicName = new string(name),
                    Name = "t/" + new string(name)
                };
            }
        }

        // topic://topic_name or topic:topic_name -> shorthand topic dispatch
        if (configuration is null && address is { Scheme: "topic" })
        {
            // topic://name → Host is the topic name
            // topic:name  → AbsolutePath is the topic name
            var name = address.Host is { Length: > 0 }
                ? address.Host
                : address.AbsolutePath.Trim('/');

            if (name.Length > 0)
            {
                configuration = new KafkaDispatchEndpointConfiguration
                {
                    TopicName = name,
                    Name = "t/" + name
                };
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
            var instanceTopicName = context.Naming.GetInstanceEndpoint(context.Host.InstanceId);
            return new KafkaReceiveEndpointConfiguration
            {
                Name = "Replies",
                TopicName = instanceTopicName,
                ConsumerGroupId = instanceTopicName,
                IsTemporary = true,
                Kind = ReceiveEndpointKind.Reply,
                AutoProvision = true,
                ReceiveMiddlewares = [ReplyReceiveMiddleware.Create()]
            };
        }

        var endpointName = context.Naming.GetReceiveEndpointName(route, ReceiveEndpointKind.Default);
        return new KafkaReceiveEndpointConfiguration
        {
            Name = endpointName,
            TopicName = endpointName,
            ConsumerGroupId = endpointName
        };
    }

    /// <inheritdoc />
    public override bool TryGetDispatchEndpoint(
        Uri address,
        [NotNullWhen(true)] out DispatchEndpoint? endpoint)
    {
        // Match by schema
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

        // Match by topology base address
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

        // Match by topic://name or topic:name shorthand
        if (address is { Scheme: "topic" })
        {
            var topicName = address.Host is { Length: > 0 }
                ? address.Host
                : address.AbsolutePath.Trim('/');

            if (topicName.Length > 0)
            {
                foreach (var candidate in DispatchEndpoints)
                {
                    if (candidate.Destination is KafkaTopic topic && topic.Name == topicName)
                    {
                        endpoint = candidate;
                        return true;
                    }
                }
            }
        }

        endpoint = null;
        return false;
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (ConnectionManager is not null)
        {
            await ConnectionManager.DisposeAsync();
        }
    }
}
