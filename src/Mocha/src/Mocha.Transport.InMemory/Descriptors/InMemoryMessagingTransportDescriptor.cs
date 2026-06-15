namespace Mocha.Transport.InMemory;

/// <summary>
/// Configures an in-memory messaging transport, including its endpoints, topics, queues, and bindings.
/// </summary>
/// <remarks>
/// This descriptor collects all receive/dispatch endpoint, topic, queue, and binding declarations
/// during setup and materializes them into an <see cref="InMemoryTransportConfiguration"/> via
/// <see cref="CreateConfiguration"/>. Use the fluent API to compose transport-level middleware,
/// naming, and handler binding strategies before the configuration is finalized.
/// </remarks>
public sealed class InMemoryMessagingTransportDescriptor
    : MessagingTransportDescriptor<InMemoryTransportConfiguration>
    , IInMemoryMessagingTransportDescriptor
{
    private readonly List<InMemoryReceiveEndpointDescriptor> _receiveEndpoints = [];
    private readonly List<InMemoryDispatchEndpointDescriptor> _dispatchEndpoints = [];
    private readonly List<InMemoryTopicDescriptor> _exchanges = [];
    private readonly List<InMemoryQueueDescriptor> _queues = [];
    private readonly List<InMemoryBindingDescriptor> _bindings = [];
    private readonly Dictionary<string, InMemoryQueueBuilder> _queueBuilders =
        new(StringComparer.Ordinal);

    /// <summary>
    /// Creates a new in-memory transport descriptor bound to the specified setup context.
    /// </summary>
    /// <param name="discoveryContext">The messaging setup context used to discover handlers and routes.</param>
    public InMemoryMessagingTransportDescriptor(IMessagingSetupContext discoveryContext) : base(discoveryContext)
    {
        Configuration = new InMemoryTransportConfiguration();
    }

    protected internal override InMemoryTransportConfiguration Configuration { get; protected set; }

    /// <inheritdoc />
    public new IInMemoryMessagingTransportDescriptor ModifyOptions(Action<TransportOptions> configure)
    {
        base.ModifyOptions(configure);

        return this;
    }

    /// <inheritdoc />
    public new IInMemoryMessagingTransportDescriptor Name(string name)
    {
        base.Name(name);

        return this;
    }

    /// <inheritdoc />
    public new IInMemoryMessagingTransportDescriptor AddConvention(IConvention convention)
    {
        base.AddConvention(convention);

        return this;
    }

    /// <inheritdoc />
    public new IInMemoryMessagingTransportDescriptor IsDefaultTransport()
    {
        base.IsDefaultTransport();

        return this;
    }

    /// <inheritdoc />
    public new IInMemoryMessagingTransportDescriptor UseDispatch(
        DispatchMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null)
    {
        base.UseDispatch(configuration, before: before, after: after);

        return this;
    }

    /// <inheritdoc />
    public new IInMemoryMessagingTransportDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null)
    {
        base.UseReceive(configuration, before: before, after: after);

        return this;
    }

    /// <inheritdoc />
    public new IInMemoryMessagingTransportDescriptor Schema(string schema)
    {
        base.Schema(schema);

        return this;
    }

    /// <inheritdoc />
    public new IInMemoryMessagingTransportDescriptor BindImplicitly()
    {
        base.BindImplicitly();

        return this;
    }

    /// <inheritdoc />
    public new IInMemoryMessagingTransportDescriptor BindExplicitly()
    {
        base.BindExplicitly();

        return this;
    }

    /// <inheritdoc />
    public IMessagingTransportHandlerDescriptor<IInMemoryReceiveEndpointDescriptor> Handler<THandler>()
        where THandler : class, IHandler
    {
        var name = Context.Naming.GetReceiveEndpointName(typeof(THandler), ReceiveEndpointKind.Default);
        var endpoint = Endpoint(name);
        endpoint.Handler(typeof(THandler));
        return new MessagingTransportHandlerDescriptor<IInMemoryReceiveEndpointDescriptor>(endpoint);
    }

    /// <inheritdoc />
    public IMessagingTransportConsumerDescriptor<IInMemoryReceiveEndpointDescriptor> Consumer<TConsumer>()
        where TConsumer : class, IConsumer
    {
        var name = Context.Naming.GetReceiveEndpointName(typeof(TConsumer), ReceiveEndpointKind.Default);
        var endpoint = Endpoint(name);
        endpoint.Consumer(typeof(TConsumer));
        return new MessagingTransportConsumerDescriptor<IInMemoryReceiveEndpointDescriptor>(endpoint);
    }

    /// <inheritdoc />
    public IInMemoryQueueBuilder Queue(string name)
    {
        if (_queueBuilders.TryGetValue(name, out var existing))
        {
            return existing;
        }

        var builder = new InMemoryQueueBuilder(this, name);
        _queueBuilders[name] = builder;
        return builder;
    }

    /// <inheritdoc />
    public IInMemoryMessagingTransportDescriptor Queue(string name, Action<IInMemoryQueueBuilder> configure)
    {
        var handle = Queue(name);
        configure(handle);
        return this;
    }

    /// <inheritdoc />
    public IInMemoryReceiveEndpointDescriptor Endpoint(string name)
    {
        var endpoint = _receiveEndpoints.FirstOrDefault(e =>
            e.Extend().Configuration.Name.EqualsOrdinal(name)
        );

        if (endpoint is null)
        {
            endpoint = InMemoryReceiveEndpointDescriptor.New(Context, name);
            _receiveEndpoints.Add(endpoint);
        }

        return endpoint;
    }

    /// <inheritdoc />
    public IInMemoryDispatchEndpointDescriptor DispatchEndpoint(string name)
    {
        var endpoint = _dispatchEndpoints.FirstOrDefault(e => e.Extend().Configuration.Name.EqualsOrdinal(name));
        if (endpoint is null)
        {
            endpoint = InMemoryDispatchEndpointDescriptor.New(Context, name);
            _dispatchEndpoints.Add(endpoint);
        }

        return endpoint;
    }

    /// <inheritdoc />
    public IInMemoryTopicDescriptor DeclareTopic(string name)
    {
        var exchange = _exchanges.FirstOrDefault(e => e.Extend().Configuration.Name.EqualsOrdinal(name));
        if (exchange is null)
        {
            exchange = InMemoryTopicDescriptor.New(Context, name);
            _exchanges.Add(exchange);
        }
        return exchange;
    }

    /// <inheritdoc />
    public IInMemoryQueueDescriptor DeclareQueue(string name)
    {
        var queue = _queues.FirstOrDefault(q => q.Extend().Configuration.Name.EqualsOrdinal(name));
        if (queue is null)
        {
            queue = InMemoryQueueDescriptor.New(Context, name);
            _queues.Add(queue);
        }
        return queue;
    }

    /// <inheritdoc />
    public IInMemoryBindingDescriptor DeclareBinding(string exchange, string queue)
    {
        var binding = _bindings.FirstOrDefault(b =>
            b.Extend().Configuration.Source.EqualsOrdinal(exchange)
            && b.Extend().Configuration.Destination.EqualsOrdinal(queue)
        );

        if (binding is null)
        {
            binding = InMemoryBindingDescriptor.New(Context, exchange, queue);
            _bindings.Add(binding);
        }

        return binding;
    }

    /// <summary>
    /// Builds the final <see cref="InMemoryTransportConfiguration"/> from all declared endpoints, topics, queues, and bindings.
    /// </summary>
    /// <returns>The fully populated transport configuration ready for runtime initialization.</returns>
    public InMemoryTransportConfiguration CreateConfiguration()
    {
        var topics = _exchanges.Select(e => e.CreateConfiguration()).ToList();
        var queues = _queues.Select(q => q.CreateConfiguration()).ToList();
        var bindings = _bindings.Select(b => b.CreateConfiguration()).ToList();

        // Prune endpoints materialized by Queue() builders that ended up entity-only (no consumer,
        // no Receives). A builder with Endpoint == null never created an endpoint, so nothing to
        // prune. A builder whose endpoint is entity-only but has satellite config (ErrorEndpoint/
        // SkippedEndpoint URIs) is an error. Otherwise, remove the phantom endpoint.
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

                if (config.ErrorEndpoint is not null)
                {
                    throw ThrowHelper.SatelliteRequiresConsumingEndpoint("error", queueName);
                }

                if (config.SkippedEndpoint is not null)
                {
                    throw ThrowHelper.SatelliteRequiresConsumingEndpoint("skipped", queueName);
                }

                // Entity-only: remove the phantom endpoint from the lifecycle list.
                _receiveEndpoints.Remove(endpoint);
            }
        }

        ValidateOneEndpointPerQueue(_receiveEndpoints);

        Configuration.Topics = topics;
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

    private static void ValidateOneEndpointPerQueue(List<InMemoryReceiveEndpointDescriptor> endpoints)
    {
        var seen = new Dictionary<string, InMemoryReceiveEndpointDescriptor>(StringComparer.Ordinal);
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
    /// Factory method that creates a new <see cref="InMemoryMessagingTransportDescriptor"/>.
    /// </summary>
    /// <param name="discoveryContext">The messaging setup context used to discover handlers and routes.</param>
    /// <returns>A new transport descriptor instance.</returns>
    public static InMemoryMessagingTransportDescriptor New(IMessagingSetupContext discoveryContext)
        => new(discoveryContext);
}
