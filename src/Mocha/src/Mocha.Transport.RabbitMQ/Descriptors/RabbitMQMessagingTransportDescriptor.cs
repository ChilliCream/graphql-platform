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
    private readonly Dictionary<string, RabbitMQQueueBuilder> _queueBuilders =
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
    public new IRabbitMQMessagingTransportDescriptor BindImplicitly()
    {
        base.BindImplicitly();

        return this;
    }

    /// <inheritdoc />
    public new IRabbitMQMessagingTransportDescriptor BindExplicitly()
    {
        base.BindExplicitly();

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
            e.Extend().Configuration.Name.EqualsOrdinal(name)
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
    public IRabbitMQQueueBuilder Queue(string name)
    {
        if (_queueBuilders.TryGetValue(name, out var existing))
        {
            return existing;
        }

        var builder = new RabbitMQQueueBuilder(this, name);
        _queueBuilders[name] = builder;
        return builder;
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

        // Prune endpoints materialized by Queue() builders that ended up entity-only (no consumer,
        // no Receives). An entity-only builder with Endpoint == null never created an endpoint, so
        // nothing to prune. A builder whose endpoint is entity-only but has satellite config is an
        // error (satellites need a consuming endpoint). Otherwise, remove the phantom endpoint.
        foreach (var builder in _queueBuilders.Values)
        {
            var endpoint = builder.Endpoint;
            if (endpoint is null)
            {
                // Infra-only builder; queue already in topology via DeclareQueue. Nothing to do.
                continue;
            }

            var config = endpoint.Configuration;
            if (config.ConsumerIdentities.Count == 0 && config.ReceivedMessageTypes.Count == 0)
            {
                var queueName = config.QueueName ?? config.Name ?? string.Empty;

                if (config.ErrorQueue.QueueName is not null || config.ErrorQueue.IsDisabled)
                {
                    throw ThrowHelper.SatelliteRequiresConsumingEndpoint("error", queueName);
                }

                if (config.SkippedQueue.QueueName is not null || config.SkippedQueue.IsDisabled)
                {
                    throw ThrowHelper.SatelliteRequiresConsumingEndpoint("skipped", queueName);
                }

                // Entity-only: remove the phantom endpoint from the lifecycle list.
                _receiveEndpoints.Remove(endpoint);
            }
        }

        ValidateOneEndpointPerQueue(_receiveEndpoints);

        Configuration.Exchanges = exchanges;
        Configuration.Queues = queues;
        Configuration.Bindings = bindings;

        Configuration.ReceiveEndpoints = _receiveEndpoints
            .Select(ReceiveEndpointConfiguration (e) => e.CreateConfiguration())
            .ToList();

        Configuration.DispatchEndpoints = _dispatchEndpoints
            .Select(DispatchEndpointConfiguration (e) => e.CreateConfiguration())
            .ToList();

        return Configuration;
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
