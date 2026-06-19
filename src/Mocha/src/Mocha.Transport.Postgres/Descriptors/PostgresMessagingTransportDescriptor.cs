using Mocha.Features;

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
    private readonly List<PostgresTopicTopologyDescriptor> _topics = [];
    private readonly List<PostgresQueueDescriptor> _queues = [];
    private readonly List<PostgresQueueTopologyDescriptor> _queueTopology = [];
    private readonly List<PostgresSubscriptionTopologyDescriptor> _subscriptions = [];

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
    public IPostgresTopicTopologyDescriptor DeclareTopic(string name)
    {
        var topic = _topics.FirstOrDefault(e => e.Extend().Configuration.Name.EqualsOrdinal(name));
        if (topic is null)
        {
            topic = PostgresTopicTopologyDescriptor.New(Context, name);
            _topics.Add(topic);
        }

        return topic;
    }

    /// <inheritdoc  />
    public IPostgresQueueTopologyDescriptor DeclareQueue(string name)
    {
        var queue = _queueTopology.FirstOrDefault(q => q.Extend().Configuration.Name.EqualsOrdinal(name));
        if (queue is null)
        {
            queue = PostgresQueueTopologyDescriptor.New(Context, name);
            _queueTopology.Add(queue);
        }

        return queue;
    }

    /// <inheritdoc  />
    public IPostgresSubscriptionTopologyDescriptor DeclareSubscription(string topic, string queue)
    {
        var subscription = _subscriptions.FirstOrDefault(b =>
            b.Extend().Configuration.Source.EqualsOrdinal(topic)
            && b.Extend().Configuration.Destination.EqualsOrdinal(queue)
        );

        if (subscription is null)
        {
            subscription = PostgresSubscriptionTopologyDescriptor.New(Context, topic, queue);
            _subscriptions.Add(subscription);
        }

        return subscription;
    }

    /// <inheritdoc />
    public IPostgresQueueDescriptor Queue(string name)
    {
        var queue = _queues.FirstOrDefault(q =>
            q.Extend().Configuration.Name.EqualsOrdinal(name));
        if (queue is not null)
        {
            return queue;
        }

        queue = PostgresQueueDescriptor.New(Context, name);
        _queues.Add(queue);
        return queue;
    }

    /// <summary>
    /// Builds the final <see cref="PostgresTransportConfiguration"/> from all declared endpoints,
    /// topics, queues, and subscriptions.
    /// </summary>
    /// <returns>The fully populated transport configuration ready for runtime initialization.</returns>
    public PostgresTransportConfiguration CreateConfiguration()
    {
        foreach (var queue in _queues.Select(q => q.CreateConfiguration()))
        {
            ConfigureQueueTopology(queue);
            ConfigureQueueEndpoint(queue);
        }

        var queues = _queueTopology.Select(q => q.CreateConfiguration()).ToList();
        var topics = _topics.Select(e => e.CreateConfiguration()).ToList();
        var subscriptions = _subscriptions.Select(b => b.CreateConfiguration()).ToList();

        var receiveEndpoints = _receiveEndpoints.Select(e => e.CreateConfiguration()).ToList();

        Configuration.Topics = topics;
        Configuration.Queues = queues;
        Configuration.Subscriptions = subscriptions;

        Configuration.ReceiveEndpoints = receiveEndpoints
            .Select(ReceiveEndpointConfiguration (e) => e)
            .ToList();

        Configuration.DispatchEndpoints = _dispatchEndpoints
            .Select(DispatchEndpointConfiguration (e) => e.CreateConfiguration())
            .ToList();

        return Configuration;
    }

    private void ConfigureQueueTopology(PostgresQueueDescriptorConfiguration configuration)
    {
        var queue = DeclareQueue(configuration.Name!);
        ApplyQueueConfiguration(configuration.Queue, queue);

        var schema = Configuration.Schema ?? PostgresTransportConfiguration.DefaultSchema;
        foreach (var source in configuration.SourceBindings)
        {
            if (!PostgresDestinations.TryResolveSourceTopic(schema, source, out var topicName))
            {
                throw new InvalidOperationException(
                    $"BindFrom source '{source}' could not be resolved to a PostgreSQL topic name.");
            }

            DeclareTopic(topicName);
            DeclareSubscription(topicName, configuration.Name!);
        }
    }

    private void ConfigureQueueEndpoint(PostgresQueueDescriptorConfiguration configuration)
    {
        var endpoint = Endpoint(configuration.Name!);
        var target = endpoint.Extend().Configuration;

        target.ConsumerIdentities.AddRange(configuration.ConsumerIdentities);
        target.ReceivedMessageTypes.AddRange(configuration.ReceivedMessageTypes);

        if (configuration.BindMode is not null)
        {
            target.BindMode ??= configuration.BindMode;
        }

        if (configuration.Kind is not null && target.Kind == ReceiveEndpointKind.Default)
        {
            target.Kind = configuration.Kind.Value;
        }

        if (configuration.MaxConcurrency is not null)
        {
            target.MaxConcurrency ??= configuration.MaxConcurrency;
        }

        if (configuration.MaxBatchSize is not null)
        {
            target.MaxBatchSize = configuration.MaxBatchSize;
        }

        target.ReceiveMiddlewares.AddRange(configuration.ReceiveMiddlewares);
        target.ReceivePipelineModifiers.AddRange(configuration.ReceivePipelineModifiers);
        CopyFaultEndpointFeature(configuration, target);
        CopySkippedEndpointFeature(configuration, target);
    }

    private static void CopyFaultEndpointFeature(
        PostgresQueueDescriptorConfiguration configuration,
        PostgresReceiveEndpointConfiguration target)
    {
        var source = configuration.Features.Get<ReceiveFaultEndpointFeature>();
        if (source is null)
        {
            return;
        }

        var targetFeature = target.Features.GetOrSet<ReceiveFaultEndpointFeature>();
        if (targetFeature is { Address: not null } or { QueueName: not null } or { IsDisabled: true })
        {
            return;
        }

        targetFeature.Address = source.Address;
        targetFeature.QueueName = source.QueueName;
        targetFeature.IsDisabled = source.IsDisabled;
    }

    private static void CopySkippedEndpointFeature(
        PostgresQueueDescriptorConfiguration configuration,
        PostgresReceiveEndpointConfiguration target)
    {
        var source = configuration.Features.Get<ReceiveSkippedEndpointFeature>();
        if (source is null)
        {
            return;
        }

        var targetFeature = target.Features.GetOrSet<ReceiveSkippedEndpointFeature>();
        if (targetFeature is { Address: not null } or { QueueName: not null } or { IsDisabled: true })
        {
            return;
        }

        targetFeature.Address = source.Address;
        targetFeature.QueueName = source.QueueName;
        targetFeature.IsDisabled = source.IsDisabled;
    }

    private static void ApplyQueueConfiguration(
        PostgresQueueConfiguration configuration,
        IPostgresQueueTopologyDescriptor descriptor)
    {
        if (configuration.AutoDelete is { } autoDelete)
        {
            descriptor.AutoDelete(autoDelete);
        }

        if (configuration.AutoProvision is { } autoProvision)
        {
            descriptor.AutoProvision(autoProvision);
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
