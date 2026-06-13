namespace Mocha.Transport.Postgres;

/// <summary>
/// Configures a PostgreSQL messaging transport, including its endpoints, topics, queues, and subscriptions.
/// </summary>
/// <remarks>
/// This descriptor collects all receive/dispatch endpoint, topic, queue, and subscription declarations
/// during setup and materializes them into a <see cref="PostgresTransportConfiguration"/> via
/// <see cref="CreateConfiguration"/>. Use the fluent API to compose transport-level middleware,
/// naming, and handler binding strategies before the configuration is finalized.
/// </remarks>
public sealed class PostgresMessagingTransportDescriptor
    : MessagingTransportDescriptor<PostgresTransportConfiguration>
        , IPostgresMessagingTransportDescriptor
{
    private readonly List<PostgresReceiveEndpointDescriptor> _receiveEndpoints = [];
    private readonly List<PostgresDispatchEndpointDescriptor> _dispatchEndpoints = [];
    private readonly List<PostgresTopicDescriptor> _topics = [];
    private readonly List<PostgresQueueDescriptor> _queues = [];
    private readonly List<PostgresSubscriptionDescriptor> _subscriptions = [];
    private readonly Dictionary<string, PostgresQueueEndpointDescriptor> _queueEndpoints =
        new(StringComparer.Ordinal);

    /// <summary>
    /// Creates a new PostgreSQL transport descriptor bound to the specified setup context.
    /// </summary>
    /// <param name="discoveryContext">The messaging setup context used to discover handlers and routes.</param>
    public PostgresMessagingTransportDescriptor(IMessagingSetupContext discoveryContext) : base(discoveryContext)
    {
        Configuration = new PostgresTransportConfiguration();
    }

    protected internal override PostgresTransportConfiguration Configuration { get; protected set; }

    /// <inheritdoc />>
    public new IPostgresMessagingTransportDescriptor ModifyOptions(Action<TransportOptions> configure)
    {
        base.ModifyOptions(configure);

        return this;
    }

    /// <inheritdoc />
    public new IPostgresMessagingTransportDescriptor Name(string name)
    {
        base.Name(name);

        return this;
    }

    /// <inheritdoc />
    public new IPostgresMessagingTransportDescriptor AddConvention(IConvention convention)
    {
        base.AddConvention(convention);

        return this;
    }

    /// <inheritdoc />
    public new IPostgresMessagingTransportDescriptor IsDefaultTransport()
    {
        base.IsDefaultTransport();

        return this;
    }

    /// <inheritdoc />
    public new IPostgresMessagingTransportDescriptor UseDispatch(
        DispatchMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null)
    {
        base.UseDispatch(configuration, before: before, after: after);

        return this;
    }

    /// <inheritdoc />
    public new IPostgresMessagingTransportDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null)
    {
        base.UseReceive(configuration, before: before, after: after);

        return this;
    }

    /// <inheritdoc />
    public new IPostgresMessagingTransportDescriptor Schema(string schema)
    {
        base.Schema(schema);

        return this;
    }

    /// <inheritdoc />
    public new IPostgresMessagingTransportDescriptor BindHandlersImplicitly()
    {
        base.BindHandlersImplicitly();

        return this;
    }

    /// <inheritdoc />
    public new IPostgresMessagingTransportDescriptor BindHandlersExplicitly()
    {
        base.BindHandlersExplicitly();

        return this;
    }

    /// <inheritdoc />
    public new IPostgresMessagingTransportDescriptor AutoBind(bool enabled)
    {
        base.AutoBind(enabled);

        return this;
    }

    /// <inheritdoc />
    public IMessagingTransportHandlerDescriptor<IPostgresReceiveEndpointDescriptor> Handler<THandler>()
        where THandler : class, IHandler
    {
        var name = Context.Naming.GetReceiveEndpointName(typeof(THandler), ReceiveEndpointKind.Default);
        var endpoint = Endpoint(name);
        endpoint.Handler(typeof(THandler));
        return new MessagingTransportHandlerDescriptor<IPostgresReceiveEndpointDescriptor>(endpoint);
    }

    /// <inheritdoc />
    public IMessagingTransportConsumerDescriptor<IPostgresReceiveEndpointDescriptor> Consumer<TConsumer>()
        where TConsumer : class, IConsumer
    {
        var name = Context.Naming.GetReceiveEndpointName(typeof(TConsumer), ReceiveEndpointKind.Default);
        var endpoint = Endpoint(name);
        endpoint.Consumer(typeof(TConsumer));
        return new MessagingTransportConsumerDescriptor<IPostgresReceiveEndpointDescriptor>(endpoint);
    }

    /// <inheritdoc />
    public IPostgresMessagingTransportDescriptor AutoProvision(bool autoProvision = true)
    {
        Configuration.AutoProvision = autoProvision;
        return this;
    }

    /// <inheritdoc  />
    public IPostgresMessagingTransportDescriptor ConnectionString(string connectionString)
    {
        Configuration.ConnectionString = connectionString;

        return this;
    }

    /// <inheritdoc  />
    public IPostgresMessagingTransportDescriptor ConfigureDefaults(Action<PostgresBusDefaults> configure)
    {
        configure(Configuration.Defaults);

        return this;
    }

    /// <inheritdoc  />
    public IPostgresReceiveEndpointDescriptor Endpoint(string name)
    {
        var endpoint = _receiveEndpoints.FirstOrDefault(e =>
            e.Extend().Configuration.Name.EqualsOrdinal(name) || e.Extend().Configuration.QueueName.EqualsOrdinal(name)
        );

        if (endpoint is null)
        {
            endpoint = PostgresReceiveEndpointDescriptor.New(Context, name);
            _receiveEndpoints.Add(endpoint);
        }

        return endpoint;
    }

    /// <inheritdoc  />
    public IPostgresDispatchEndpointDescriptor DispatchEndpoint(string name)
    {
        var endpoint = _dispatchEndpoints.FirstOrDefault(e => e.Extend().Configuration.Name.EqualsOrdinal(name));
        if (endpoint is null)
        {
            endpoint = PostgresDispatchEndpointDescriptor.New(Context, name);
            _dispatchEndpoints.Add(endpoint);
        }

        return endpoint;
    }

    /// <inheritdoc  />
    public IPostgresTopicDescriptor DeclareTopic(string name)
    {
        var topic = _topics.FirstOrDefault(e => e.Extend().Configuration.Name.EqualsOrdinal(name));
        if (topic is null)
        {
            topic = PostgresTopicDescriptor.New(Context, name);
            _topics.Add(topic);
        }

        return topic;
    }

    /// <inheritdoc  />
    public IPostgresQueueDescriptor DeclareQueue(string name)
    {
        var queue = _queues.FirstOrDefault(q => q.Extend().Configuration.Name.EqualsOrdinal(name));
        if (queue is null)
        {
            queue = PostgresQueueDescriptor.New(Context, name);
            _queues.Add(queue);
        }

        return queue;
    }

    /// <inheritdoc  />
    public IPostgresSubscriptionDescriptor DeclareSubscription(string topic, string queue)
    {
        var subscription = _subscriptions.FirstOrDefault(b =>
            b.Extend().Configuration.Source.EqualsOrdinal(topic)
            && b.Extend().Configuration.Destination.EqualsOrdinal(queue)
        );

        if (subscription is null)
        {
            subscription = PostgresSubscriptionDescriptor.New(Context, topic, queue);
            _subscriptions.Add(subscription);
        }

        return subscription;
    }

    /// <inheritdoc />
    public IPostgresQueueEndpointDescriptor Queue(string name)
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
            backing = PostgresReceiveEndpointDescriptor.New(Context, name);
            _receiveEndpoints.Add(backing);
        }

        var adapter = new PostgresQueueEndpointDescriptor(backing);
        _queueEndpoints[name] = adapter;
        return adapter;
    }

    /// <inheritdoc />
    public IPostgresMessagingTransportDescriptor Queue(string name, Action<IPostgresQueueEndpointDescriptor> configure)
    {
        var handle = Queue(name);
        configure(handle);
        return this;
    }

    /// <summary>
    /// Builds the final <see cref="PostgresTransportConfiguration"/> from all declared endpoints,
    /// topics, queues, and subscriptions.
    /// </summary>
    /// <returns>The fully populated transport configuration ready for runtime initialization.</returns>
    public PostgresTransportConfiguration CreateConfiguration()
    {
        var queues = _queues.Select(q => q.CreateConfiguration()).ToList();
        var topics = _topics.Select(e => e.CreateConfiguration()).ToList();
        var subscriptions = _subscriptions.Select(b => b.CreateConfiguration()).ToList();

        // Partition the unified Queue() handles: an entity-only handle (no consumers, no Receives)
        // is a pure dispatch target. It lowers to a declared queue plus its BindFrom subscriptions
        // here and never enters the receive-endpoint lifecycle. A handle that names a consumer or a
        // received type materializes a real receive endpoint and stays in the list below.
        var entityOnly = new HashSet<PostgresReceiveEndpointDescriptor>();
        var resolver = new PostgresDestinationResolver(
            Configuration.Schema ?? PostgresTransportConfiguration.DefaultSchema);
        foreach (var adapter in _queueEndpoints.Values)
        {
            var backing = adapter.Inner;
            if (IsEntityOnly(backing.Configuration))
            {
                LowerEntityOnlyQueue(resolver, backing.Configuration, queues, topics, subscriptions);
                entityOnly.Add(backing);
            }
        }

        var consumingEndpoints = _receiveEndpoints
            .Where(e => !entityOnly.Contains(e))
            .ToList();

        ValidateOneEndpointPerQueue(consumingEndpoints);

        Configuration.Topics = topics;
        Configuration.Queues = queues;
        Configuration.Subscriptions = subscriptions;

        Configuration.ReceiveEndpoints = consumingEndpoints
            .Select(ReceiveEndpointConfiguration (e) => e.CreateConfiguration())
            .ToList();

        Configuration.DispatchEndpoints = _dispatchEndpoints
            .Select(DispatchEndpointConfiguration (e) => e.CreateConfiguration())
            .ToList();

        return Configuration;
    }

    private static bool IsEntityOnly(PostgresReceiveEndpointConfiguration configuration)
        => configuration.ConsumerIdentities.Count == 0
            && configuration.ReceivedMessageTypes.Count == 0;

    private void LowerEntityOnlyQueue(
        PostgresDestinationResolver resolver,
        PostgresReceiveEndpointConfiguration configuration,
        List<PostgresQueueConfiguration> queues,
        List<PostgresTopicConfiguration> topics,
        List<PostgresSubscriptionConfiguration> subscriptions)
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

        // Lower the queue itself (queue before subscription, matching the transport's initialization
        // order where queues are added before subscriptions reference them).
        queues.Add(
            new PostgresQueueConfiguration
            {
                Name = queueName,
                AutoProvision = configuration.AutoProvision ?? Configuration.AutoProvision
            });

        // Materialize the queue-level BindFrom intents into declared topic-to-queue subscriptions,
        // the same lowering the receive-endpoint lifecycle performs for a consuming endpoint.
        foreach (var intent in configuration.QueueBindFroms)
        {
            if (intent.RoutingKey is not null)
            {
                throw ThrowHelper.BindFromWithNonNullRoutingKey(
                    "PostgreSQL",
                    intent.Source.ToString(),
                    queueName);
            }

            if (!resolver.TryResolveSourceTopic(intent.Source, out var topicName))
            {
                throw new InvalidOperationException(
                    $"BindFrom source '{intent.Source}' could not be resolved to a PostgreSQL topic name.");
            }

            // Ensure the source topic exists. AddTopic merges on duplicate names via the runtime path,
            // so for the descriptor-time lowering we use a simple existence check on the list.
            if (topics.All(t => t.Name != topicName))
            {
                topics.Add(new PostgresTopicConfiguration { Name = topicName });
            }

            // Add the subscription only if it does not already exist.
            if (subscriptions.All(s => s.Source != topicName || s.Destination != queueName))
            {
                subscriptions.Add(
                    new PostgresSubscriptionConfiguration
                    {
                        Source = topicName,
                        Destination = queueName,
                        AutoProvision = Configuration.AutoProvision
                    });
            }
        }
    }

    private static void ValidateOneEndpointPerQueue(List<PostgresReceiveEndpointDescriptor> endpoints)
    {
        var seen = new Dictionary<string, PostgresReceiveEndpointDescriptor>(StringComparer.Ordinal);
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
    /// Factory method that creates a new <see cref="PostgresMessagingTransportDescriptor"/>.
    /// </summary>
    /// <param name="discoveryContext">The messaging setup context used to discover handlers and routes.</param>
    /// <returns>A new transport descriptor instance.</returns>
    public static PostgresMessagingTransportDescriptor New(IMessagingSetupContext discoveryContext)
        => new(discoveryContext);
}
