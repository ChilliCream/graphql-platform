using System.Diagnostics.CodeAnalysis;
using Mocha.Features;
using static System.StringSplitOptions;

namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Azure Service Bus implementation of <see cref="MessagingTransport"/> that manages connections,
/// topology provisioning, and the lifecycle of receive and dispatch endpoints backed by
/// Azure Service Bus queues and topics.
/// </summary>
public sealed class AzureServiceBusMessagingTransport : MessagingTransport
{
    private readonly Action<IAzureServiceBusMessagingTransportDescriptor> _configure;

    /// <summary>
    /// Creates a new Azure Service Bus transport with the specified configuration delegate.
    /// </summary>
    /// <param name="configure">A delegate that configures the transport descriptor with endpoints, topology, and connection settings.</param>
    public AzureServiceBusMessagingTransport(Action<IAzureServiceBusMessagingTransportDescriptor> configure)
    {
        _configure = configure;
    }

    private AzureServiceBusMessagingTopology _topology = null!;

    /// <inheritdoc />
    public override MessagingTopology Topology => _topology;

    /// <summary>
    /// Gets the client manager responsible for managing the <c>ServiceBusClient</c> and cached senders.
    /// </summary>
    public AzureServiceBusClientManager ClientManager { get; private set; } = null!;

    /// <summary>
    /// Resolves the Azure Service Bus connection settings, creates the client manager, and builds the
    /// transport topology from the declared topics, queues, and subscriptions.
    /// </summary>
    /// <param name="context">The setup context providing access to the service provider and host configuration.</param>
    protected override void OnAfterInitialized(IMessagingSetupContext context)
    {
        var configuration = (AzureServiceBusTransportConfiguration)Configuration;

        ClientManager = new AzureServiceBusClientManager(configuration);

        var builder = new UriBuilder
        {
            Scheme = Schema,
            Host = configuration.FullyQualifiedNamespace ?? "localhost",
            Path = "/"
        };

        _topology = new AzureServiceBusMessagingTopology(
            this,
            builder.Uri,
            configuration.Defaults,
            configuration.AutoProvision ?? true);

        foreach (var topic in configuration.Topics)
        {
            _topology.AddTopic(topic);
        }

        foreach (var queue in configuration.Queues)
        {
            _topology.AddQueue(queue);
        }

        foreach (var subscription in configuration.Subscriptions)
        {
            _topology.AddSubscription(subscription);
        }
    }

    /// <summary>
    /// Provisions topology resources (topics, queues, subscriptions) on the Azure Service Bus namespace
    /// before the transport's endpoints begin processing messages.
    /// </summary>
    /// <param name="context">The configuration context for the current startup phase.</param>
    /// <param name="cancellationToken">A token to cancel the provisioning.</param>
    protected override async ValueTask OnBeforeStartAsync(
        IMessagingConfigurationContext context,
        CancellationToken cancellationToken)
    {
        var autoProvision = _topology.AutoProvision;

        foreach (var topic in _topology.Topics)
        {
            if (topic.AutoProvision ?? autoProvision)
            {
                await topic.ProvisionAsync(ClientManager, cancellationToken);
            }
        }

        foreach (var queue in _topology.Queues)
        {
            if (queue.AutoProvision ?? autoProvision)
            {
                await queue.ProvisionAsync(ClientManager, cancellationToken);
            }
        }

        foreach (var subscription in _topology.Subscriptions)
        {
            if (subscription.AutoProvision ?? autoProvision)
            {
                await subscription.ProvisionAsync(ClientManager, cancellationToken);
            }
        }
    }

    /// <inheritdoc />
    public override TransportDescription Describe()
    {
        var receiveEndpoints = ReceiveEndpoints.Select(e => e.Describe()).ToList();
        var dispatchEndpoints = DispatchEndpoints.Select(e => e.Describe()).ToList();

        var entities = new List<TopologyEntityDescription>();
        var links = new List<TopologyLinkDescription>();
        var autoProvision = _topology.AutoProvision;

        foreach (var topic in _topology.Topics)
        {
            entities.Add(
                new TopologyEntityDescription(
                    "topic",
                    topic.Name,
                    topic.Address?.ToString(),
                    "inbound",
                    new Dictionary<string, object?>
                    {
                        ["autoProvision"] = topic.AutoProvision ?? autoProvision
                    }));
        }

        foreach (var queue in _topology.Queues)
        {
            entities.Add(
                new TopologyEntityDescription(
                    "queue",
                    queue.Name,
                    queue.Address?.ToString(),
                    "outbound",
                    new Dictionary<string, object?>
                    {
                        ["autoDelete"] = queue.AutoDelete,
                        ["autoProvision"] = queue.AutoProvision ?? autoProvision
                    }));
        }

        foreach (var subscription in _topology.Subscriptions)
        {
            links.Add(
                new TopologyLinkDescription(
                    "subscription",
                    subscription.Address?.ToString(),
                    subscription.Source.Address?.ToString(),
                    subscription.Destination.Address?.ToString(),
                    "forward",
                    new Dictionary<string, object?>
                    {
                        ["autoProvision"] = subscription.AutoProvision ?? autoProvision
                    }));
        }

        var topology = new TopologyDescription(_topology.Address.ToString(), entities, links);

        return new TransportDescription(
            _topology.Address.ToString(),
            Name,
            Schema,
            nameof(AzureServiceBusMessagingTransport),
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

        // Convenience schemes: support both host-based (queue://name) and path-based (queue:///name) URIs
        if (address is { Scheme: "queue", Host: { Length: > 0 } queueName })
        {
            foreach (var candidate in DispatchEndpoints)
            {
                if (candidate.Destination is AzureServiceBusQueue queue && queue.Name == queueName)
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
                if (candidate.Destination is AzureServiceBusTopic topic && topic.Name == topicName)
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
    /// Builds the Azure Service Bus-specific transport configuration by invoking the user-supplied
    /// configuration delegate on an <see cref="AzureServiceBusMessagingTransportDescriptor"/>.
    /// </summary>
    /// <param name="context">The setup context providing access to the service provider and host configuration.</param>
    /// <returns>A <see cref="MessagingTransportConfiguration"/> containing all Azure Service Bus endpoint and pipeline definitions.</returns>
    protected override MessagingTransportConfiguration CreateConfiguration(IMessagingSetupContext context)
    {
        var descriptor = new AzureServiceBusMessagingTransportDescriptor(context);

        _configure(descriptor);

        return descriptor.CreateConfiguration();
    }

    /// <summary>
    /// Creates a new <see cref="AzureServiceBusReceiveEndpoint"/> bound to this transport.
    /// </summary>
    /// <returns>A new, uninitialized <see cref="AzureServiceBusReceiveEndpoint"/> for this transport.</returns>
    protected override ReceiveEndpoint CreateReceiveEndpoint()
    {
        return new AzureServiceBusReceiveEndpoint(this);
    }

    /// <summary>
    /// Creates a new <see cref="AzureServiceBusDispatchEndpoint"/> bound to this transport.
    /// </summary>
    /// <returns>A new, uninitialized <see cref="AzureServiceBusDispatchEndpoint"/> for this transport.</returns>
    protected override DispatchEndpoint CreateDispatchEndpoint()
    {
        return new AzureServiceBusDispatchEndpoint(this);
    }

    /// <inheritdoc />
    public override DispatchEndpointConfiguration? CreateEndpointConfiguration(
        IMessagingConfigurationContext context,
        OutboundRoute route)
    {
        AzureServiceBusDispatchEndpointConfiguration? configuration = null;
        if (route.Kind == OutboundRouteKind.Send)
        {
            var queueName = context.Naming.GetSendEndpointName(route.MessageType.RuntimeType);
            configuration = new AzureServiceBusDispatchEndpointConfiguration
            {
                QueueName = queueName,
                Name = "q/" + queueName
            };
        }
        else if (route.Kind == OutboundRouteKind.Publish)
        {
            var topicName = context.Naming.GetPublishEndpointName(route.MessageType.RuntimeType);
            configuration = new AzureServiceBusDispatchEndpointConfiguration
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
        AzureServiceBusDispatchEndpointConfiguration? configuration = null;

        var path = address.AbsolutePath.AsSpan();
        Span<Range> ranges = stackalloc Range[2];
        var segmentCount = path.Split(ranges, '/', RemoveEmptyEntries | TrimEntries);

        // Handle azuresb:///replies and azuresb:///t/{name} / azuresb:///q/{name}
        if (address.Scheme == Schema && address.Host is "")
        {
            if (segmentCount == 1 && path[ranges[0]] is "replies")
            {
                var instanceEndpointName = context.Naming.GetInstanceEndpoint(context.Host.InstanceId);
                configuration = new AzureServiceBusDispatchEndpointConfiguration
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

                if (kind is "t")
                {
                    configuration = new AzureServiceBusDispatchEndpointConfiguration
                    {
                        TopicName = new string(name),
                        Name = "t/" + new string(name)
                    };
                }

                if (kind is "q")
                {
                    configuration = new AzureServiceBusDispatchEndpointConfiguration
                    {
                        QueueName = new string(name),
                        Name = "q/" + new string(name)
                    };
                }
            }
        }

        // Handle full topology URLs
        if (configuration is null && _topology.Address.IsBaseOf(address) && segmentCount == 2)
        {
            var kind = path[ranges[0]];
            var name = path[ranges[1]];

            if (kind is "t")
            {
                configuration = new AzureServiceBusDispatchEndpointConfiguration
                {
                    TopicName = new string(name),
                    Name = "t/" + new string(name)
                };
            }

            if (kind is "q")
            {
                configuration = new AzureServiceBusDispatchEndpointConfiguration
                {
                    QueueName = new string(name),
                    Name = "q/" + new string(name)
                };
            }
        }

        // Handle convenience schemes (supports both host-based and path-based URIs)
        if (configuration is null && address is { Scheme: "queue" })
        {
            var name =
                !string.IsNullOrEmpty(address.Host) ? address.Host
                : segmentCount == 1 ? new string(path[ranges[0]]) : null;

            if (name is not null)
            {
                configuration = new AzureServiceBusDispatchEndpointConfiguration
                {
                    QueueName = name,
                    Name = "q/" + name
                };
            }
        }

        if (configuration is null && address is { Scheme: "topic" })
        {
            var name =
                !string.IsNullOrEmpty(address.Host) ? address.Host
                : segmentCount == 1 ? new string(path[ranges[0]]) : null;

            if (name is not null)
            {
                configuration = new AzureServiceBusDispatchEndpointConfiguration
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
        AzureServiceBusReceiveEndpointConfiguration configuration;
        if (route.Kind == InboundRouteKind.Reply)
        {
            var instanceEndpointName = context.Naming.GetInstanceEndpoint(context.Host.InstanceId);
            configuration = new AzureServiceBusReceiveEndpointConfiguration
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
            configuration = new AzureServiceBusReceiveEndpointConfiguration { Name = queueName, QueueName = queueName };
        }

        return configuration;
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (ClientManager is not null)
        {
            await ClientManager.DisposeAsync();
        }
    }
}
