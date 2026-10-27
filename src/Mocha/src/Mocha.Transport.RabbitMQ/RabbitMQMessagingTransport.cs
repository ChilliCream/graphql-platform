using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

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

    /// <inheritdoc />
    public override MessagingTopology Topology => _topology;

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

        return new RabbitMQConsumerManager(logger, CreateConnectionFactory(context, "consumer"));
    }

    private RabbitMQDispatcher CreateDispatcher(IMessagingSetupContext context)
    {
        var logger = context.Services.GetRequiredService<ILogger<RabbitMQDispatcher>>();

        return new RabbitMQDispatcher(logger, CreateConnectionFactory(context, "dispatcher"), ProvisionTopologyAsync);

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

            // An exclusive queue belongs to the connection that declares it, so only its consumer declares it and
            // the bindings that target it.
            foreach (var queue in _topology.Queues)
            {
                if (!queue.Exclusive && (queue.AutoProvision ?? autoProvision))
                {
                    await queue.ProvisionAsync(channel, ct);
                }
            }

            foreach (var binding in _topology.Bindings)
            {
                if (binding is RabbitMQQueueBinding { Destination.Exclusive: true })
                {
                    continue;
                }

                if (binding.AutoProvision ?? autoProvision)
                {
                    await binding.ProvisionAsync(channel, ct);
                }
            }
        }
    }

    private Func<CancellationToken, ValueTask<IConnection>> CreateConnectionFactory(
        IMessagingSetupContext context,
        string role)
    {
        if (Connection is not ConnectionFactoryRabbitMQConnectionProvider provider)
        {
            return Connection.CreateAsync;
        }

        // Each connection is named after the service and its role, so the two can be told apart on the broker.
        var clientProvidedName = $"{context.Host.ServiceName ?? context.Host.ProcessName} {role}";
        return cancellationToken => provider.CreateAsync(clientProvidedName, cancellationToken);
    }

    /// <inheritdoc />
    public override TransportHealth GetHealth()
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (ConsumerManager is null || Dispatcher is null)
        {
            return TransportHealth.Healthy;
        }

        List<TransportHealthEntry> entries =
        [
            ConsumerManager.GetConnectionHealth("connection:consumer"),
            Dispatcher.GetConnectionHealth("connection:dispatcher"),
            .. ConsumerManager.GetConsumerHealth()
        ];

        return new TransportHealth(entries);
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
                    MochaUrn.TopologyEntity(exchange.Address?.ToString(), "exchange", exchange.Name),
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
                        ["origin"] = exchange.Origin
                    }));
        }

        foreach (var queue in _topology.Queues)
        {
            entities.Add(
                new TopologyEntityDescription(
                    MochaUrn.TopologyEntity(queue.Address?.ToString(), "queue", queue.Name),
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
                        ["origin"] = queue.Origin
                    }));
        }

        foreach (var binding in _topology.Bindings)
        {
            var source = binding.Source.Address?.ToString();
            var target = binding switch
            {
                RabbitMQQueueBinding qb => qb.Destination.Address?.ToString(),
                RabbitMQExchangeBinding eb => eb.Destination.Address?.ToString(),
                _ => null
            };

            links.Add(
                new TopologyLinkDescription(
                    MochaUrn.TopologyLink(binding.Address?.ToString(), "bind", source, target),
                    "bind",
                    binding.Address?.ToString(),
                    source,
                    target,
                    "forward",
                    new Dictionary<string, object?>
                    {
                        ["routingKey"] = string.IsNullOrEmpty(binding.RoutingKey) ? null : binding.RoutingKey,
                        ["autoProvision"] = binding.AutoProvision ?? autoProvision,
                        ["origin"] = binding.Origin
                    }));
        }

        var topology = new TopologyDescription(_topology.Address.ToString(), entities, links);

        return new TransportDescription(
            Urn,
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
    /// Connects the consumer and dispatcher RabbitMQ connections before the transport's endpoints start.
    /// </summary>
    /// <remarks>
    /// When the broker cannot be reached, both connections keep connecting in the background and the
    /// transport starts anyway: consumers attach once the broker is reachable, and publishing fails until then.
    /// Refused credentials, refused virtual host access, and topology the broker rejects fail the start.
    /// </remarks>
    /// <param name="context">The configuration context for the current startup phase.</param>
    /// <param name="cancellationToken">A token to cancel the connection establishment.</param>
    protected override async ValueTask OnBeforeStartAsync(
        IMessagingConfigurationContext context,
        CancellationToken cancellationToken)
    {
        await Task.WhenAll(
            ConsumerManager.ConnectOrRetryInBackgroundAsync(cancellationToken),
            Dispatcher.ConnectOrRetryInBackgroundAsync(cancellationToken));
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
        List<Exception>? exceptions = null;

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (ConsumerManager is not null)
        {
            try
            {
                await ConsumerManager.DisposeAsync();
            }
            catch (Exception ex)
            {
                (exceptions ??= []).Add(ex);
            }
        }

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (Dispatcher is not null)
        {
            try
            {
                await Dispatcher.DisposeAsync();
            }
            catch (Exception ex)
            {
                (exceptions ??= []).Add(ex);
            }
        }

        if (exceptions is not null)
        {
            throw new AggregateException(exceptions);
        }
    }
}
