using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using static System.StringSplitOptions;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// RabbitMQ implementation of <see cref="MessagingTransport"/> that manages connections, topology provisioning,
/// and the lifecycle of receive and dispatch endpoints backed by RabbitMQ queues and exchanges.
/// </summary>
public sealed class RabbitMQMessagingTransport : MessagingTransport
{
    private readonly Action<IRabbitMQMessagingTransportDescriptor> _configure;

    /// <summary>
    /// Creates a new RabbitMQ transport with the specified configuration delegate.
    /// </summary>
    /// <param name="configure">A delegate that configures the transport descriptor with endpoints, topology, and connection settings.</param>
    public RabbitMQMessagingTransport(Action<IRabbitMQMessagingTransportDescriptor> configure)
    {
        _configure = configure;
    }

    private RabbitMQMessagingTopology _topology = null!;
    private RabbitMQDestinationResolver? _resolver;

    /// <inheritdoc />
    public override MessagingTopology Topology => _topology;

    /// <summary>
    /// Gets the destination resolver consulted by both the producer path and the receive convention so
    /// the two sides converge on one entity and cannot drift apart.
    /// </summary>
    internal RabbitMQDestinationResolver Resolver => _resolver ??= new RabbitMQDestinationResolver(Schema);

    /// <summary>
    /// Gets the consumer manager responsible for registering and maintaining queue consumers with automatic reconnection.
    /// </summary>
    public RabbitMQConsumerManager ConsumerManager { get; private set; } = null!;

    /// <summary>
    /// Gets the dispatcher that provides pooled channels for publishing messages to RabbitMQ.
    /// </summary>
    public RabbitMQDispatcher Dispatcher { get; private set; } = null!;

    /// <summary>
    /// Gets the connection provider used to create RabbitMQ connections.
    /// </summary>
    public IRabbitMQConnectionProvider Connection { get; private set; } = null!;

    /// <summary>
    /// Resolves the RabbitMQ connection provider, builds the transport topology URI, and creates
    /// the <see cref="Dispatcher"/> and <see cref="ConsumerManager"/> instances used for the
    /// lifetime of this transport.
    /// </summary>
    /// <remarks>
    /// Called once during the messaging host initialization phase, after the base transport has
    /// been initialized. If no custom <see cref="IRabbitMQConnectionProvider"/> was registered in
    /// configuration, a default provider backed by <see cref="IConnectionFactory"/> from DI is used.
    /// The topology URI is constructed from the connection host, port, and virtual host so that
    /// every endpoint address can be resolved relative to this transport.
    /// </remarks>
    /// <param name="context">The setup context providing access to the service provider and host configuration.</param>
    protected override void OnAfterInitialized(IMessagingSetupContext context)
    {
        var configuration = (RabbitMQTransportConfiguration)Configuration;

        Connection =
            configuration.ConnectionProvider?.Invoke(context.Services)
            ?? new ConnectionFactoryRabbitMQConnectionProvider(
                context.Services.GetApplicationServices().GetRequiredService<IConnectionFactory>());

        var builder = new UriBuilder
        {
            Scheme = Schema,
            Host = Connection.Host,
            Port = Connection.Port,
            Path = Connection.VirtualHost
        };
        _topology = new RabbitMQMessagingTopology(
            this,
            builder.Uri,
            configuration.Defaults,
            configuration.AutoProvision ?? true);

        foreach (var exchange in configuration.Exchanges)
        {
            _topology.AddExchange(exchange);
        }

        foreach (var queue in configuration.Queues)
        {
            _topology.AddQueue(queue);
        }

        foreach (var binding in configuration.Bindings)
        {
            _topology.AddBinding(binding);
        }

        Dispatcher = CreateDispatcher(context);
        ConsumerManager = CreateConsumerManager(context);
    }

    private RabbitMQConsumerManager CreateConsumerManager(IMessagingSetupContext context)
    {
        var logger = context.Services.GetRequiredService<ILogger<RabbitMQConsumerManager>>();

        return new RabbitMQConsumerManager(logger, Connection.CreateAsync);
    }

    private RabbitMQDispatcher CreateDispatcher(IMessagingSetupContext context)
    {
        var logger = context.Services.GetRequiredService<ILogger<RabbitMQDispatcher>>();

        return new RabbitMQDispatcher(logger, Connection.CreateAsync, ProvisionTopologyAsync);

        async Task ProvisionTopologyAsync(IConnection connection, CancellationToken ct)
        {
            await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
            var autoProvision = _topology.AutoProvision;

            foreach (var exchange in _topology.Exchanges)
            {
                if (exchange.AutoProvision ?? autoProvision)
                {
                    await exchange.ProvisionAsync(channel, ct);
                }
            }

            foreach (var queue in _topology.Queues)
            {
                if (queue.AutoProvision ?? autoProvision)
                {
                    await queue.ProvisionAsync(channel, ct);
                }
            }

            foreach (var binding in _topology.Bindings)
            {
                if (binding.AutoProvision ?? autoProvision)
                {
                    await binding.ProvisionAsync(channel, ct);
                }
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

        foreach (var exchange in _topology.Exchanges)
        {
            entities.Add(
                new TopologyEntityDescription(
                    "exchange",
                    exchange.Name,
                    exchange.Address?.ToString(),
                    "inbound",
                    new Dictionary<string, object?>
                    {
                        ["type"] = exchange.Type,
                        ["durable"] = exchange.Durable,
                        ["autoDelete"] = exchange.AutoDelete,
                        ["autoProvision"] = exchange.AutoProvision ?? autoProvision,
                        ["source"] = exchange.Provenance
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
                        ["durable"] = queue.Durable,
                        ["exclusive"] = queue.Exclusive,
                        ["autoDelete"] = queue.AutoDelete,
                        ["autoProvision"] = queue.AutoProvision ?? autoProvision,
                        ["source"] = queue.Provenance
                    }));
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
                        RabbitMQQueueBinding qb => qb.Destination.Address?.ToString(),
                        RabbitMQExchangeBinding eb => eb.Destination.Address?.ToString(),
                        _ => null
                    },
                    "forward",
                    new Dictionary<string, object?>
                    {
                        ["routingKey"] = string.IsNullOrEmpty(binding.RoutingKey) ? null : binding.RoutingKey,
                        ["autoProvision"] = binding.AutoProvision ?? autoProvision,
                        ["source"] = binding.Provenance
                    }));
        }

        var topology = new TopologyDescription(_topology.Address.ToString(), entities, links);

        return new TransportDescription(
            _topology.Address.ToString(),
            Name,
            Schema,
            nameof(RabbitMQMessagingTransport),
            receiveEndpoints,
            dispatchEndpoints,
            topology);
    }

    /// <inheritdoc />
    public override bool TryGetDispatchEndpoint(Uri address, [NotNullWhen(true)] out DispatchEndpoint? endpoint)
    {
        if (TryGetReplyDispatchEndpoint(address, out endpoint))
        {
            return true;
        }

        if (address.Scheme == Schema)
        {
            foreach (var candidate in DispatchEndpoints)
            {
                if (!candidate.IsCompleted)
                {
                    continue;
                }

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
                if (!candidate.IsCompleted)
                {
                    continue;
                }

                if (candidate.Destination.Address == address)
                {
                    endpoint = candidate;
                    return true;
                }
            }
        }

        if (TryGetResourceName(address, "queue", out var queueName))
        {
            foreach (var candidate in DispatchEndpoints)
            {
                if (!candidate.IsCompleted)
                {
                    continue;
                }

                if (candidate.Destination is RabbitMQQueue queue && queue.Name == queueName)
                {
                    endpoint = candidate;
                    return true;
                }
            }
        }

        if (TryGetResourceName(address, "exchange", out var exchangeName))
        {
            foreach (var candidate in DispatchEndpoints)
            {
                if (!candidate.IsCompleted)
                {
                    continue;
                }

                if (candidate.Destination is RabbitMQExchange exchange
                    && exchange.Name == exchangeName)
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
    /// Ensures that both the consumer and dispatcher RabbitMQ connections are established before
    /// the transport's endpoints begin processing messages.
    /// </summary>
    /// <remarks>
    /// Both <see cref="ConsumerManager"/> and <see cref="Dispatcher"/> connect concurrently via
    /// <c>Task.WhenAll</c>. If either connection fails, the start-up will fail and the host will
    /// not begin consuming or dispatching messages. This is the last opportunity to guarantee
    /// network connectivity before the messaging pipeline is active.
    /// </remarks>
    /// <param name="context">The configuration context for the current startup phase.</param>
    /// <param name="cancellationToken">A token to cancel the connection establishment.</param>
    protected override async ValueTask OnBeforeStartAsync(
        IMessagingConfigurationContext context,
        CancellationToken cancellationToken)
    {
        // TODO we probably should make this resilient!
        await Task.WhenAll(
            ConsumerManager.EnsureConnectedAsync(cancellationToken),
            Dispatcher.EnsureConnectedAsync(cancellationToken));
    }

    /// <summary>
    /// Builds the RabbitMQ-specific transport configuration by invoking the user-supplied
    /// configuration delegate on a <see cref="RabbitMQMessagingTransportDescriptor"/>.
    /// </summary>
    /// <remarks>
    /// The descriptor collects endpoint definitions, topology declarations, middleware, and
    /// conventions, then produces a <see cref="MessagingTransportConfiguration"/> that the base
    /// class uses to wire up receive and dispatch pipelines.
    /// </remarks>
    /// <param name="context">The setup context providing access to the service provider and host configuration.</param>
    /// <returns>A <see cref="MessagingTransportConfiguration"/> containing all RabbitMQ endpoint and pipeline definitions.</returns>
    protected override MessagingTransportConfiguration CreateConfiguration(IMessagingSetupContext context)
    {
        var descriptor = new RabbitMQMessagingTransportDescriptor(context);

        _configure(descriptor);

        return descriptor.CreateConfiguration();
    }

    /// <inheritdoc />
    protected override RoutingStrategy CreateRoutingStrategy()
    {
        return new RabbitMQRoutingStrategy();
    }

    /// <summary>
    /// Creates a new <see cref="RabbitMQReceiveEndpoint"/> bound to this transport, which will
    /// consume messages from a RabbitMQ queue via the <see cref="ConsumerManager"/>.
    /// </summary>
    /// <returns>A new, uninitialized <see cref="RabbitMQReceiveEndpoint"/> for this transport.</returns>
    protected override ReceiveEndpoint CreateReceiveEndpoint()
    {
        return new RabbitMQReceiveEndpoint(this);
    }

    /// <summary>
    /// Creates a new <see cref="RabbitMQDispatchEndpoint"/> bound to this transport, which will
    /// publish messages to RabbitMQ exchanges or queues via the <see cref="Dispatcher"/>.
    /// </summary>
    /// <returns>A new, uninitialized <see cref="RabbitMQDispatchEndpoint"/> for this transport.</returns>
    protected override DispatchEndpoint CreateDispatchEndpoint()
    {
        return new RabbitMQDispatchEndpoint(this);
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (ConsumerManager is not null)
        {
            await ConsumerManager.DisposeAsync();
        }
    }
}
