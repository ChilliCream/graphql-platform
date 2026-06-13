namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Fluent descriptor for configuring a RabbitMQ messaging transport, including endpoints, topology, and connection settings.
/// </summary>
public sealed class RabbitMQMessagingTransportDescriptor
    : MessagingTransportDescriptor<RabbitMQTransportConfiguration>
    , IRabbitMQMessagingTransportDescriptor
{
    private readonly List<RabbitMQReceiveEndpointDescriptor> _receiveEndpoints = [];
    private readonly List<RabbitMQDispatchEndpointDescriptor> _dispatchEndpoints = [];
    private readonly List<RabbitMQExchangeDescriptor> _exchanges = [];
    private readonly List<RabbitMQQueueDescriptor> _queues = [];
    private readonly List<RabbitMQBindingDescriptor> _bindings = [];
    private readonly Dictionary<string, RabbitMQQueueEndpointDescriptor> _queueEndpoints =
        new(StringComparer.Ordinal);

    /// <summary>
    /// Creates a new RabbitMQ transport descriptor bound to the given setup context.
    /// </summary>
    /// <param name="discoveryContext">The messaging setup context used for handler and route discovery.</param>
    public RabbitMQMessagingTransportDescriptor(IMessagingSetupContext discoveryContext) : base(discoveryContext)
    {
        Configuration = new RabbitMQTransportConfiguration();
    }

    protected internal override RabbitMQTransportConfiguration Configuration { get; protected set; }

    /// <inheritdoc />
    public new IRabbitMQMessagingTransportDescriptor ModifyOptions(Action<TransportOptions> configure)
    {
        base.ModifyOptions(configure);

        return this;
    }

    /// <inheritdoc />
    public new IRabbitMQMessagingTransportDescriptor Name(string name)
    {
        base.Name(name);

        return this;
    }

    /// <inheritdoc />
    public new IRabbitMQMessagingTransportDescriptor AddConvention(IConvention convention)
    {
        base.AddConvention(convention);

        return this;
    }

    /// <inheritdoc />
    public new IRabbitMQMessagingTransportDescriptor IsDefaultTransport()
    {
        base.IsDefaultTransport();

        return this;
    }

    /// <inheritdoc />
    public new IRabbitMQMessagingTransportDescriptor UseDispatch(
        DispatchMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null)
    {
        base.UseDispatch(configuration, before, after);

        return this;
    }

    /// <inheritdoc />
    public new IRabbitMQMessagingTransportDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null)
    {
        base.UseReceive(configuration, before, after);

        return this;
    }

    /// <inheritdoc />
    public new IRabbitMQMessagingTransportDescriptor Schema(string schema)
    {
        base.Schema(schema);

        return this;
    }

    /// <inheritdoc />
    public new IRabbitMQMessagingTransportDescriptor BindHandlersImplicitly()
    {
        base.BindHandlersImplicitly();

        return this;
    }

    /// <inheritdoc />
    public new IRabbitMQMessagingTransportDescriptor BindHandlersExplicitly()
    {
        base.BindHandlersExplicitly();

        return this;
    }

    /// <inheritdoc />
    public new IRabbitMQMessagingTransportDescriptor AutoBind(bool enabled)
    {
        base.AutoBind(enabled);

        return this;
    }

    /// <inheritdoc />
    public IMessagingTransportHandlerDescriptor<IRabbitMQReceiveEndpointDescriptor> Handler<THandler>()
        where THandler : class, IHandler
    {
        var name = Context.Naming.GetReceiveEndpointName(typeof(THandler), ReceiveEndpointKind.Default);
        var endpoint = Endpoint(name);
        endpoint.Handler(typeof(THandler));
        return new MessagingTransportHandlerDescriptor<IRabbitMQReceiveEndpointDescriptor>(endpoint);
    }

    /// <inheritdoc />
    public IMessagingTransportConsumerDescriptor<IRabbitMQReceiveEndpointDescriptor> Consumer<TConsumer>()
        where TConsumer : class, IConsumer
    {
        var name = Context.Naming.GetReceiveEndpointName(typeof(TConsumer), ReceiveEndpointKind.Default);
        var endpoint = Endpoint(name);
        endpoint.Consumer(typeof(TConsumer));
        return new MessagingTransportConsumerDescriptor<IRabbitMQReceiveEndpointDescriptor>(endpoint);
    }

    /// <inheritdoc />
    public IRabbitMQMessagingTransportDescriptor AutoProvision(bool autoProvision = true)
    {
        Configuration.AutoProvision = autoProvision;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQMessagingTransportDescriptor ConnectionProvider(
        Func<IServiceProvider, IRabbitMQConnectionProvider> connectionProvider)
    {
        Configuration.ConnectionProvider = connectionProvider;

        return this;
    }

    /// <inheritdoc />
    public IRabbitMQMessagingTransportDescriptor ConfigureDefaults(Action<RabbitMQBusDefaults> configure)
    {
        configure(Configuration.Defaults);

        return this;
    }

    /// <inheritdoc />
    public IRabbitMQReceiveEndpointDescriptor Endpoint(string name)
    {
        var endpoint = _receiveEndpoints.FirstOrDefault(e =>
            e.Extend().Configuration.Name.EqualsOrdinal(name) || e.Extend().Configuration.QueueName.EqualsOrdinal(name)
        );

        if (endpoint is null)
        {
            endpoint = RabbitMQReceiveEndpointDescriptor.New(Context, name);
            _receiveEndpoints.Add(endpoint);
        }

        return endpoint;
    }

    /// <inheritdoc />
    public IRabbitMQDispatchEndpointDescriptor DispatchEndpoint(string name)
    {
        var endpoint = _dispatchEndpoints.FirstOrDefault(e => e.Extend().Configuration.Name.EqualsOrdinal(name));
        if (endpoint is null)
        {
            endpoint = RabbitMQDispatchEndpointDescriptor.New(Context, name);
            _dispatchEndpoints.Add(endpoint);
        }

        return endpoint;
    }

    /// <inheritdoc />
    public IRabbitMQExchangeDescriptor DeclareExchange(string name)
    {
        var exchange = _exchanges.FirstOrDefault(e => e.Extend().Configuration.Name.EqualsOrdinal(name));
        if (exchange is null)
        {
            exchange = RabbitMQExchangeDescriptor.New(Context, name);
            _exchanges.Add(exchange);
        }
        return exchange;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor DeclareQueue(string name)
    {
        var queue = _queues.FirstOrDefault(q => q.Extend().Configuration.Name.EqualsOrdinal(name));
        if (queue is null)
        {
            queue = RabbitMQQueueDescriptor.New(Context, name);
            _queues.Add(queue);
        }
        return queue;
    }

    /// <inheritdoc />
    public IRabbitMQBindingDescriptor DeclareBinding(string exchange, string queue)
    {
        var binding = RabbitMQBindingDescriptor.New(Context, exchange, queue);
        _bindings.Add(binding);
        return binding;
    }

    /// <inheritdoc />
    public IRabbitMQQueueEndpointDescriptor Queue(string name)
    {
        if (_queueEndpoints.TryGetValue(name, out var existing))
        {
            return existing;
        }

        // Locate an endpoint whose effective queue name already matches. This merges onto
        // an endpoint that was previously created via Endpoint("foo").Queue(name).
        var backing = _receiveEndpoints.FirstOrDefault(e =>
            e.Extend().Configuration.QueueName.EqualsOrdinal(name));

        if (backing is null)
        {
            backing = RabbitMQReceiveEndpointDescriptor.New(Context, name);
            _receiveEndpoints.Add(backing);
        }

        var adapter = new RabbitMQQueueEndpointDescriptor(backing);
        _queueEndpoints[name] = adapter;
        return adapter;
    }

    /// <inheritdoc />
    public IRabbitMQMessagingTransportDescriptor Queue(string name, Action<IRabbitMQQueueEndpointDescriptor> configure)
    {
        var handle = Queue(name);
        configure(handle);
        return this;
    }

    /// <summary>
    /// Builds the final transport configuration from all accumulated descriptor settings, including receive and dispatch endpoints.
    /// </summary>
    /// <returns>A fully populated <see cref="RabbitMQTransportConfiguration"/> ready for transport initialization.</returns>
    public RabbitMQTransportConfiguration CreateConfiguration()
    {
        var exchanges = _exchanges.Select(e => e.CreateConfiguration()).ToList();
        var queues = _queues.Select(q => q.CreateConfiguration()).ToList();
        var bindings = _bindings.Select(b => b.CreateConfiguration()).ToList();

        // Partition the unified Queue() handles: an entity-only handle (no consumers, no Receives)
        // is a pure dispatch target. It lowers to a declared queue plus its BindFrom bindings here
        // and never enters the receive-endpoint lifecycle. A handle that names a consumer or a
        // received type materializes a real receive endpoint and stays in the list below.
        var entityOnly = new HashSet<RabbitMQReceiveEndpointDescriptor>();
        var resolver = new RabbitMQDestinationResolver(
            Configuration.Schema ?? RabbitMQTransportConfiguration.DefaultSchema);
        foreach (var adapter in _queueEndpoints.Values)
        {
            var backing = adapter.Inner;
            if (IsEntityOnly(backing.Configuration))
            {
                LowerEntityOnlyQueue(resolver, backing.Configuration, queues, exchanges, bindings);
                entityOnly.Add(backing);
            }
        }

        var consumingEndpoints = _receiveEndpoints
            .Where(e => !entityOnly.Contains(e))
            .ToList();

        ValidateOneEndpointPerQueue(consumingEndpoints);

        Configuration.Exchanges = exchanges;
        Configuration.Queues = queues;
        Configuration.Bindings = bindings;

        Configuration.ReceiveEndpoints = consumingEndpoints
            .Select(ReceiveEndpointConfiguration (e) => e.CreateConfiguration())
            .ToList();

        Configuration.DispatchEndpoints = _dispatchEndpoints
            .Select(DispatchEndpointConfiguration (e) => e.CreateConfiguration())
            .ToList();

        return Configuration;
    }

    private static bool IsEntityOnly(RabbitMQReceiveEndpointConfiguration configuration)
        => configuration.ConsumerIdentities.Count == 0
            && configuration.ReceivedMessageTypes.Count == 0;

    private void LowerEntityOnlyQueue(
        RabbitMQDestinationResolver resolver,
        RabbitMQReceiveEndpointConfiguration configuration,
        List<RabbitMQQueueConfiguration> queues,
        List<RabbitMQExchangeConfiguration> exchanges,
        List<RabbitMQBindingConfiguration> bindings)
    {
        var queueName = configuration.QueueName
            ?? throw new InvalidOperationException("Queue name is required.");

        // Satellites (error, skipped) require a consuming endpoint to process the failed or skipped
        // messages. An entity-only queue has no consumer, so a configured satellite cannot be honored.
        if (configuration.ErrorQueue.QueueName is not null || configuration.ErrorQueue.IsDisabled)
        {
            throw ThrowHelper.SatelliteRequiresConsumingEndpoint("error", queueName);
        }

        if (configuration.SkippedQueue.QueueName is not null || configuration.SkippedQueue.IsDisabled)
        {
            throw ThrowHelper.SatelliteRequiresConsumingEndpoint("skipped", queueName);
        }

        // Lower the queue itself (queue before binding, matching the transport's initialization order
        // where queues are added before bindings reference them).
        queues.Add(
            new RabbitMQQueueConfiguration
            {
                Name = queueName,
                Durable = configuration.QueueDurable,
                Arguments = configuration.QueueArguments,
                AutoProvision = configuration.QueueAutoProvision ?? Configuration.AutoProvision,
                Provenance = RabbitMQTopologyProvenance.Declared
            });

        // Materialize the queue-level BindFrom intents into declared exchange-to-queue bindings, the
        // same lowering the receive-endpoint lifecycle performs for a consuming endpoint.
        foreach (var intent in configuration.QueueBindFroms)
        {
            if (!resolver.TryResolveSourceExchange(intent.Source, out var exchangeName))
            {
                throw new InvalidOperationException(
                    $"BindFrom source '{intent.Source}' could not be resolved to a RabbitMQ exchange name.");
            }

            exchanges.Add(
                new RabbitMQExchangeConfiguration
                {
                    Name = exchangeName,
                    AutoProvision = Configuration.AutoProvision,
                    Provenance = RabbitMQTopologyProvenance.Declared
                });

            bindings.Add(
                new RabbitMQBindingConfiguration
                {
                    Source = exchangeName,
                    Destination = queueName,
                    DestinationKind = RabbitMQDestinationKind.Queue,
                    RoutingKey = intent.RoutingKey,
                    AutoProvision = Configuration.AutoProvision,
                    Provenance = RabbitMQTopologyProvenance.Declared
                });
        }
    }

    private static void ValidateOneEndpointPerQueue(List<RabbitMQReceiveEndpointDescriptor> endpoints)
    {
        var seen = new Dictionary<string, RabbitMQReceiveEndpointDescriptor>(StringComparer.Ordinal);
        foreach (var endpoint in endpoints)
        {
            var queueName = endpoint.Configuration.QueueName;
            if (queueName is null)
            {
                continue;
            }

            if (seen.TryGetValue(queueName, out var existing))
            {
                throw ThrowHelper.TwoReceiveEndpointsShareOneQueue(
                    queueName,
                    existing.Configuration.Name ?? queueName,
                    endpoint.Configuration.Name ?? queueName);
            }

            seen[queueName] = endpoint;
        }
    }

    /// <summary>
    /// Creates a new <see cref="RabbitMQMessagingTransportDescriptor"/> for the given setup context.
    /// </summary>
    /// <param name="discoveryContext">The messaging setup context.</param>
    /// <returns>A new transport descriptor instance.</returns>
    public static RabbitMQMessagingTransportDescriptor New(IMessagingSetupContext discoveryContext)
        => new(discoveryContext);
}
