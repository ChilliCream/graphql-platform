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
    public new IInMemoryMessagingTransportDescriptor BindHandlersImplicitly()
    {
        base.BindHandlersImplicitly();

        return this;
    }

    /// <inheritdoc />
    public new IInMemoryMessagingTransportDescriptor BindHandlersExplicitly()
    {
        base.BindHandlersExplicitly();

        return this;
    }

    /// <inheritdoc />
    public new IInMemoryMessagingTransportDescriptor AutoBind(bool enabled)
    {
        base.AutoBind(enabled);

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
        var queues = _queues.Select(q => q.CreateConfiguration()).ToList();
        var topics = _exchanges.Select(e => e.CreateConfiguration()).ToList();
        var bindings = _bindings.Select(b => b.CreateConfiguration()).ToList();

        // Partition the unified Queue() handles: an entity-only handle (no consumers, no Receives)
        // is a pure dispatch target. It lowers to a declared queue plus its BindFrom bindings here
        // and never enters the receive-endpoint lifecycle. A handle that names a consumer or a
        // received type materializes a real receive endpoint and stays in the list below.
        var entityOnly = new HashSet<InMemoryReceiveEndpointDescriptor>();
        var resolver = new InMemoryDestinationResolver(
            Configuration.Schema ?? InMemoryTransportConfiguration.DefaultSchema);
        foreach (var adapter in _queueEndpoints.Values)
        {
            var backing = adapter.Inner;
            if (IsEntityOnly(backing.Configuration))
            {
                LowerEntityOnlyQueue(resolver, backing.Configuration, queues, topics, bindings);
                entityOnly.Add(backing);
            }
        }

        var consumingEndpoints = _receiveEndpoints
            .Where(e => !entityOnly.Contains(e))
            .ToList();

        ValidateOneEndpointPerQueue(consumingEndpoints);

        Configuration.Topics = topics;
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

    private static bool IsEntityOnly(InMemoryReceiveEndpointConfiguration configuration)
        => configuration.ConsumerIdentities.Count == 0
            && configuration.ReceivedMessageTypes.Count == 0;

    private static void LowerEntityOnlyQueue(
        InMemoryDestinationResolver resolver,
        InMemoryReceiveEndpointConfiguration configuration,
        List<InMemoryQueueConfiguration> queues,
        List<InMemoryTopicConfiguration> topics,
        List<InMemoryBindingConfiguration> bindings)
    {
        var queueName = configuration.QueueName
            ?? throw new InvalidOperationException("Queue name is required.");

        // Satellites require a consuming endpoint to process failed or skipped messages.
        // An entity-only queue has no consumer, so a configured satellite cannot be honored.
        if (configuration.ErrorEndpoint is not null)
        {
            throw ThrowHelper.SatelliteRequiresConsumingEndpoint("error", queueName);
        }

        if (configuration.SkippedEndpoint is not null)
        {
            throw ThrowHelper.SatelliteRequiresConsumingEndpoint("skipped", queueName);
        }

        // Lower the queue itself.
        queues.Add(new InMemoryQueueConfiguration { Name = queueName });

        // Materialize the queue-level BindFrom intents into topic-to-queue bindings.
        foreach (var intent in configuration.QueueBindFroms)
        {
            if (intent.RoutingKey is not null)
            {
                throw ThrowHelper.BindFromWithNonNullRoutingKey(
                    "in-memory",
                    intent.Source.ToString(),
                    queueName);
            }

            if (!resolver.TryResolveSourceTopic(intent.Source, out var topicName))
            {
                throw new InvalidOperationException(
                    $"BindFrom source '{intent.Source}' could not be resolved to an in-memory topic name.");
            }

            // Ensure the source topic exists.
            if (topics.All(t => t.Name != topicName))
            {
                topics.Add(new InMemoryTopicConfiguration { Name = topicName });
            }

            // Add the binding only if it does not already exist.
            if (bindings.All(b => b.Source != topicName || b.Destination != queueName))
            {
                bindings.Add(
                    new InMemoryBindingConfiguration
                    {
                        Source = topicName,
                        Destination = queueName,
                        DestinationKind = InMemoryDestinationKind.Queue
                    });
            }
        }
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
