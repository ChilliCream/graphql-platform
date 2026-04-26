using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha.Features;
using Mocha.Scheduling;
using Mocha.Transport.Postgres.Tasks;
using static System.StringSplitOptions;

namespace Mocha.Transport.Postgres;

/// <summary>
/// PostgreSQL implementation of <see cref="MessagingTransport"/> that manages connections, topology provisioning,
/// and the lifecycle of receive and dispatch endpoints backed by PostgreSQL tables and LISTEN/NOTIFY signaling.
/// </summary>
public sealed class PostgresMessagingTransport : MessagingTransport
{
    private readonly Action<IPostgresMessagingTransportDescriptor> _configure;
    private readonly PostgresBackgroundTaskScheduler _backgroundTasks = new();
    private IReadOnlyPostgresSchemaOptions _schemaOptions = null!;

    /// <summary>
    /// Creates a new PostgreSQL transport with the specified configuration delegate.
    /// </summary>
    /// <param name="configure">A delegate that configures the transport descriptor with endpoints, topology, and connection settings.</param>
    public PostgresMessagingTransport(Action<IPostgresMessagingTransportDescriptor> configure)
    {
        _configure = configure;
    }

    private PostgresMessagingTopology _topology = null!;

    /// <inheritdoc />
    public override MessagingTopology Topology => _topology;

    /// <summary>
    /// Gets the connection manager responsible for providing PostgreSQL connections.
    /// </summary>
    public PostgresConnectionManager ConnectionManager { get; private set; } = null!;

    /// <summary>
    /// Gets the notification listener that subscribes to PostgreSQL LISTEN/NOTIFY events.
    /// </summary>
    public PostgresNotificationListener NotificationListener { get; private set; } = null!;

    /// <summary>
    /// Gets the message store that handles database operations for storing and retrieving messages.
    /// </summary>
    public PostgresMessageStore MessageStore { get; private set; } = null!;

    /// <summary>
    /// Gets the consumer manager responsible for consumer lifecycle (registration, heartbeat, cleanup).
    /// </summary>
    public PostgresConsumerManager ConsumerManager { get; private set; } = null!;

    /// <summary>
    /// Resolves the PostgreSQL connection string from configuration, creates the connection manager,
    /// notification listener, and message store instances, and builds the topology.
    /// </summary>
    /// <param name="context">The setup context providing access to the service provider and host configuration.</param>
    protected override void OnAfterInitialized(IMessagingSetupContext context)
    {
        var configuration = (PostgresTransportConfiguration)Configuration;

        if (string.IsNullOrEmpty(configuration.ConnectionString))
        {
            throw new InvalidOperationException("PostgreSQL connection string is required");
        }

        _schemaOptions = configuration.SchemaOptions;

        var connectionManagerLogger = context.Services.GetRequiredService<ILogger<PostgresConnectionManager>>();
        ConnectionManager = new PostgresConnectionManager(
            configuration.ConnectionString,
            configuration.SchemaOptions,
            connectionManagerLogger);

        var listenerLogger = context.Services.GetRequiredService<ILogger<PostgresNotificationListener>>();
        NotificationListener = new PostgresNotificationListener(
            ConnectionManager,
            configuration.SchemaOptions,
            listenerLogger);

        MessageStore = new PostgresMessageStore(ConnectionManager, configuration.SchemaOptions);
        ConsumerManager = new PostgresConsumerManager(
            configuration.Name ?? PostgresTransportConfiguration.DefaultName,
            ConnectionManager,
            _schemaOptions);

        var builder = new UriBuilder
        {
            Scheme = Schema,
            Host = configuration.Host,
            Port = configuration.Port,
            Path = "/"
        };
        _topology = new PostgresMessagingTopology(
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

        Features.Configure<SchedulingTransportFeature>(f => f.SupportsSchedulingNatively = true);
    }

    /// <summary>
    /// Ensures database migrations are run, registers the consumer, starts the notification listener,
    /// provisions topology resources, and starts background tasks before the transport's endpoints
    /// begin processing messages.
    /// </summary>
    /// <param name="context">The configuration context for the current startup phase.</param>
    /// <param name="cancellationToken">A token to cancel the startup.</param>
    protected override async ValueTask OnBeforeStartAsync(
        IMessagingConfigurationContext context,
        CancellationToken cancellationToken)
    {
        await ConnectionManager.EnsureMigratedAsync(cancellationToken);
        await ConsumerManager.RegisterAsync(cancellationToken);
        await NotificationListener.StartAsync(cancellationToken);

        // Provision topology resources
        var autoProvision = _topology.AutoProvision;

        foreach (var topic in _topology.Topics)
        {
            if (topic.AutoProvision ?? autoProvision)
            {
                await topic.ProvisionAsync(ConnectionManager, _schemaOptions, cancellationToken);
            }
        }

        foreach (var queue in _topology.Queues)
        {
            if (queue.AutoProvision ?? autoProvision)
            {
                await queue.ProvisionAsync(ConnectionManager, _schemaOptions, ConsumerManager, cancellationToken);
            }
        }

        foreach (var subscription in _topology.Subscriptions)
        {
            if (subscription.AutoProvision ?? autoProvision)
            {
                await subscription.ProvisionAsync(ConnectionManager, _schemaOptions, cancellationToken);
            }
        }

        // Register and start background tasks
        _backgroundTasks.Add(
            new ConsumerHeartbeatTask(
                ConsumerManager,
                ConnectionManager,
                _schemaOptions,
                context.Services.GetRequiredService<ILogger<ConsumerHeartbeatTask>>(),
                RecoverFromEvictionAsync));
        _backgroundTasks.Add(
            new ExpiredConsumerCleanupTask(
                ConsumerManager,
                ConnectionManager,
                _schemaOptions,
                context.Services.GetRequiredService<ILogger<ExpiredConsumerCleanupTask>>()));
        _backgroundTasks.Add(
            new OrphanedMessageCleanupTask(
                ConnectionManager,
                _schemaOptions,
                context.Services.GetRequiredService<ILogger<OrphanedMessageCleanupTask>>()));
        _backgroundTasks.Add(
            new QueueOverflowCleanupTask(
                ConnectionManager,
                _schemaOptions,
                context.Services.GetRequiredService<ILogger<QueueOverflowCleanupTask>>()));
        _backgroundTasks.Add(
            new QueueMonitoringTask(
                ConnectionManager,
                _schemaOptions,
                context.Services.GetRequiredService<ILogger<QueueMonitoringTask>>()));
        _backgroundTasks.Add(
            new TopicMonitoringTask(
                ConnectionManager,
                _schemaOptions,
                context.Services.GetRequiredService<ILogger<TopicMonitoringTask>>()));
        _backgroundTasks.Add(
            new MessageCleanupTask(
                ConnectionManager,
                _schemaOptions,
                context.Services.GetRequiredService<ILogger<MessageCleanupTask>>()));

        _backgroundTasks.Start();
    }

    /// <inheritdoc />
    internal override TransportDescription Describe()
    {
        var receiveEndpoints = ReceiveEndpoints.Select(e => e.Describe()).ToList();
        var dispatchEndpoints = DispatchEndpoints.Select(e => e.Describe()).ToList();

        var entities = new List<TopologyEntityDescription>();
        var links = new List<TopologyLinkDescription>();

        foreach (var topic in _topology.Topics)
        {
            entities.Add(
                new TopologyEntityDescription(
                    "topic",
                    topic.Name,
                    topic.Address.ToString(),
                    "inbound",
                    new Dictionary<string, object?> { ["autoProvision"] = topic.AutoProvision }));
        }

        foreach (var queue in _topology.Queues)
        {
            entities.Add(
                new TopologyEntityDescription(
                    "queue",
                    queue.Name,
                    queue.Address.ToString(),
                    "outbound",
                    new Dictionary<string, object?>
                    {
                        ["autoDelete"] = queue.AutoDelete,
                        ["autoProvision"] = queue.AutoProvision
                    }));
        }

        foreach (var subscription in _topology.Subscriptions)
        {
            links.Add(
                new TopologyLinkDescription(
                    "subscription",
                    subscription.Address.ToString(),
                    subscription.Source.Address.ToString(),
                    subscription.Destination.Address.ToString(),
                    "forward",
                    new Dictionary<string, object?> { ["autoProvision"] = subscription.AutoProvision }));
        }

        var topology = new TopologyDescription(_topology.Address.ToString(), entities, links);

        return new TransportDescription(
            _topology.Address.ToString(),
            Name,
            Schema,
            nameof(PostgresMessagingTransport),
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
                if (candidate.Destination is PostgresQueue queue && queue.Name == queueName)
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
                if (candidate.Destination is PostgresTopic topic && topic.Name == topicName)
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
    /// Builds the PostgreSQL-specific transport configuration by invoking the user-supplied
    /// configuration delegate on a <see cref="PostgresMessagingTransportDescriptor"/>.
    /// </summary>
    /// <param name="context">The setup context providing access to the service provider and host configuration.</param>
    /// <returns>A <see cref="MessagingTransportConfiguration"/> containing all PostgreSQL endpoint and pipeline definitions.</returns>
    protected override MessagingTransportConfiguration CreateConfiguration(IMessagingSetupContext context)
    {
        var descriptor = new PostgresMessagingTransportDescriptor(context);

        _configure(descriptor);

        return descriptor.CreateConfiguration();
    }

    /// <summary>
    /// Creates a new <see cref="PostgresReceiveEndpoint"/> bound to this transport.
    /// </summary>
    /// <returns>A new, uninitialized <see cref="PostgresReceiveEndpoint"/> for this transport.</returns>
    protected override ReceiveEndpoint CreateReceiveEndpoint()
    {
        return new PostgresReceiveEndpoint(this);
    }

    /// <summary>
    /// Creates a new <see cref="PostgresDispatchEndpoint"/> bound to this transport.
    /// </summary>
    /// <returns>A new, uninitialized <see cref="PostgresDispatchEndpoint"/> for this transport.</returns>
    protected override DispatchEndpoint CreateDispatchEndpoint()
    {
        return new PostgresDispatchEndpoint(this);
    }

    /// <inheritdoc />
    public override DispatchEndpointConfiguration? CreateEndpointConfiguration(
        IMessagingConfigurationContext context,
        OutboundRoute route)
    {
        PostgresDispatchEndpointConfiguration? configuration = null;
        if (route.Kind == OutboundRouteKind.Send)
        {
            var queueName = context.Naming.GetSendEndpointName(route.MessageType.RuntimeType);
            configuration = new PostgresDispatchEndpointConfiguration
            {
                QueueName = queueName,
                Name = "q/" + queueName
            };
        }
        else if (route.Kind == OutboundRouteKind.Publish)
        {
            var topicName = context.Naming.GetPublishEndpointName(route.MessageType.RuntimeType);
            configuration = new PostgresDispatchEndpointConfiguration
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
        PostgresDispatchEndpointConfiguration? configuration = null;

        var path = address.AbsolutePath.AsSpan();
        Span<Range> ranges = stackalloc Range[2];
        var segmentCount = path.Split(ranges, '/', RemoveEmptyEntries | TrimEntries);

        if (address.Scheme == Schema && address.Host is "")
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

        if (configuration is null && _topology.Address.IsBaseOf(address) && segmentCount == 2)
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

    /// <summary>
    /// Recovers from consumer eviction by re-registering the consumer, re-provisioning
    /// all queues (idempotent via ON CONFLICT DO UPDATE), and re-provisioning all
    /// subscriptions (CASCADE-deleted temp queue subs need to be restored).
    /// </summary>
    private async Task RecoverFromEvictionAsync(CancellationToken cancellationToken)
    {
        await ConsumerManager.RegisterAsync(cancellationToken);

        foreach (var queue in _topology.Queues)
        {
            await queue.ProvisionAsync(ConnectionManager, _schemaOptions, ConsumerManager, cancellationToken);
        }

        foreach (var subscription in _topology.Subscriptions)
        {
            await subscription.ProvisionAsync(ConnectionManager, _schemaOptions, cancellationToken);
        }
    }

    /// <inheritdoc />
    protected override async ValueTask OnBeforeStopAsync(CancellationToken cancellationToken)
    {
        await _backgroundTasks.DisposeAsync();

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (ConsumerManager is not null)
        {
            try
            {
                await ConsumerManager.UnregisterAsync(cancellationToken);
            }
            catch
            {
                // Best-effort unregistration during shutdown
            }
        }
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        await _backgroundTasks.DisposeAsync();

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (NotificationListener is not null)
        {
            await NotificationListener.DisposeAsync();
        }

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (ConnectionManager is not null)
        {
            await ConnectionManager.DisposeAsync();
        }
    }
}
