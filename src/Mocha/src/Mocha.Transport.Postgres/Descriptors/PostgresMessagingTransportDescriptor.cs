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
    public new IPostgresMessagingTransportDescriptor UseDispatch(DispatchMiddlewareConfiguration configuration)
    {
        base.UseDispatch(configuration);

        return this;
    }

    /// <inheritdoc />
    public new IPostgresMessagingTransportDescriptor AppendDispatch(
        string after,
        DispatchMiddlewareConfiguration configuration)
    {
        base.AppendDispatch(after, configuration);

        return this;
    }

    /// <inheritdoc />
    public new IPostgresMessagingTransportDescriptor PrependDispatch(
        string before,
        DispatchMiddlewareConfiguration configuration)
    {
        base.PrependDispatch(before, configuration);

        return this;
    }

    /// <inheritdoc />
    public new IPostgresMessagingTransportDescriptor UseReceive(ReceiveMiddlewareConfiguration configuration)
    {
        base.UseReceive(configuration);

        return this;
    }

    /// <inheritdoc />
    public new IPostgresMessagingTransportDescriptor AppendReceive(
        string after,
        ReceiveMiddlewareConfiguration configuration)
    {
        base.AppendReceive(after, configuration);

        return this;
    }

    /// <inheritdoc />
    public new IPostgresMessagingTransportDescriptor PrependReceive(
        string before,
        ReceiveMiddlewareConfiguration configuration)
    {
        base.PrependReceive(before, configuration);

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

    /// <summary>
    /// Builds the final <see cref="PostgresTransportConfiguration"/> from all declared endpoints,
    /// topics, queues, and subscriptions.
    /// </summary>
    /// <returns>The fully populated transport configuration ready for runtime initialization.</returns>
    public PostgresTransportConfiguration CreateConfiguration()
    {
        Configuration.ReceiveEndpoints = _receiveEndpoints
            .Select(ReceiveEndpointConfiguration (e) => e.CreateConfiguration())
            .ToList();

        Configuration.DispatchEndpoints = _dispatchEndpoints
            .Select(DispatchEndpointConfiguration (e) => e.CreateConfiguration())
            .ToList();

        Configuration.Topics = _topics.Select(e => e.CreateConfiguration()).ToList();

        Configuration.Queues = _queues.Select(q => q.CreateConfiguration()).ToList();

        Configuration.Subscriptions = _subscriptions.Select(b => b.CreateConfiguration()).ToList();

        return Configuration;
    }

    /// <summary>
    /// Factory method that creates a new <see cref="PostgresMessagingTransportDescriptor"/>.
    /// </summary>
    /// <param name="discoveryContext">The messaging setup context used to discover handlers and routes.</param>
    /// <returns>A new transport descriptor instance.</returns>
    public static PostgresMessagingTransportDescriptor New(IMessagingSetupContext discoveryContext)
        => new(discoveryContext);
}
