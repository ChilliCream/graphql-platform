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
    private readonly Dictionary<string, PostgresQueueBuilder> _queueBuilders =
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
    public new IPostgresMessagingTransportDescriptor UseRoutingStrategy(Func<IServiceProvider, RoutingStrategy> factory)
    {
        base.UseRoutingStrategy(factory);

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
    public new IPostgresMessagingTransportDescriptor BindImplicitly()
    {
        base.BindImplicitly();

        return this;
    }

    /// <inheritdoc />
    public new IPostgresMessagingTransportDescriptor BindExplicitly()
    {
        base.BindExplicitly();

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
            e.Extend().Configuration.Name.EqualsOrdinal(name)
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
    public IPostgresQueueBuilder Queue(string name)
    {
        if (_queueBuilders.TryGetValue(name, out var existing))
        {
            return existing;
        }

        var builder = new PostgresQueueBuilder(this, name);
        _queueBuilders[name] = builder;
        return builder;
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

        Configuration.Topics = topics;
        Configuration.Queues = queues;
        Configuration.Subscriptions = subscriptions;

        Configuration.ReceiveEndpoints = _receiveEndpoints
            .Select(ReceiveEndpointConfiguration (e) => e.CreateConfiguration())
            .ToList();

        Configuration.DispatchEndpoints = _dispatchEndpoints
            .Select(DispatchEndpointConfiguration (e) => e.CreateConfiguration())
            .ToList();

        return Configuration;
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
