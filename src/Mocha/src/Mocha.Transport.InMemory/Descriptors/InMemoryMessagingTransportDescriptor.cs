using Mocha.Features;

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
    private readonly List<InMemoryTopicTopologyDescriptor> _exchanges = [];
    private readonly List<InMemoryQueueDescriptor> _queues = [];
    private readonly List<InMemoryQueueTopologyDescriptor> _queueTopology = [];
    private readonly List<InMemoryBindingTopologyDescriptor> _bindings = [];

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
    public new IInMemoryMessagingTransportDescriptor UseRoutingStrategy(Func<IServiceProvider, RoutingStrategy> factory)
    {
        base.UseRoutingStrategy(factory);

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
    public IInMemoryQueueDescriptor Queue(string name)
    {
        var queue = _queues.FirstOrDefault(q => q.Configuration.Name.EqualsOrdinal(name));
        if (queue is not null)
        {
            return queue;
        }

        queue = InMemoryQueueDescriptor.New(Context, name);
        _queues.Add(queue);
        return queue;
    }

    /// <inheritdoc />
    public IInMemoryReceiveEndpointDescriptor Endpoint(string name)
    {
        var endpoint = _receiveEndpoints.FirstOrDefault(e => e.Configuration.Name.EqualsOrdinal(name));

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
        var endpoint = _dispatchEndpoints.FirstOrDefault(e => e.Configuration.Name.EqualsOrdinal(name));
        if (endpoint is null)
        {
            endpoint = InMemoryDispatchEndpointDescriptor.New(Context, name);
            _dispatchEndpoints.Add(endpoint);
        }

        return endpoint;
    }

    /// <inheritdoc />
    public IInMemoryTopicTopologyDescriptor DeclareTopic(string name)
    {
        var exchange = _exchanges.FirstOrDefault(e => e.Configuration.Name.EqualsOrdinal(name));
        if (exchange is null)
        {
            exchange = InMemoryTopicTopologyDescriptor.New(Context, name);
            _exchanges.Add(exchange);
        }
        return exchange;
    }

    /// <inheritdoc />
    public IInMemoryQueueTopologyDescriptor DeclareQueue(string name)
    {
        var queue = _queueTopology.FirstOrDefault(q => q.Configuration.Name.EqualsOrdinal(name));
        if (queue is null)
        {
            queue = InMemoryQueueTopologyDescriptor.New(Context, name);
            _queueTopology.Add(queue);
        }
        return queue;
    }

    /// <inheritdoc />
    public IInMemoryBindingTopologyDescriptor DeclareBinding(string exchange, string queue)
    {
        var binding = _bindings.FirstOrDefault(b =>
            b.Configuration.Source.EqualsOrdinal(exchange)
            && b.Configuration.Destination.EqualsOrdinal(queue)
        );

        if (binding is null)
        {
            binding = InMemoryBindingTopologyDescriptor.New(Context, exchange, queue);
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
        foreach (var queue in _queues.Select(q => q.CreateConfiguration()))
        {
            ConfigureQueueTopology(queue);
            if (IsEntityOnly(queue))
            {
                ValidateEntityOnlyQueue(queue);
                continue;
            }

            ConfigureQueueEndpoint(queue);
        }

        var topics = _exchanges.Select(e => e.CreateConfiguration()).ToList();
        var queues = _queueTopology.Select(q => q.CreateConfiguration()).ToList();
        var bindings = _bindings.Select(b => b.CreateConfiguration()).ToList();

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

    private void ConfigureQueueTopology(InMemoryQueueDescriptorConfiguration configuration)
    {
        DeclareQueue(configuration.Name!);

        var schema = Configuration.Schema ?? InMemoryTransportConfiguration.DefaultSchema;
        foreach (var source in configuration.SourceBindings)
        {
            if (!InMemoryDestinations.TryResolveSourceTopic(schema, source, out var topicName))
            {
                throw new InvalidOperationException(
                    $"BindFrom source '{source}' could not be resolved to an in-memory topic name.");
            }

            DeclareTopic(topicName);
            DeclareBinding(topicName, configuration.Name!);
        }
    }

    private void ConfigureQueueEndpoint(InMemoryQueueDescriptorConfiguration configuration)
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

        target.ReceiveMiddlewares.AddRange(configuration.ReceiveMiddlewares);
        target.ReceivePipelineModifiers.AddRange(configuration.ReceivePipelineModifiers);
        CopyFaultEndpointFeature(configuration, target);
        CopySkippedEndpointFeature(configuration, target);
    }

    private static bool IsEntityOnly(InMemoryQueueDescriptorConfiguration configuration)
        => configuration.ConsumerIdentities.Count == 0
            && configuration.ReceivedMessageTypes.Count == 0;

    private static void ValidateEntityOnlyQueue(InMemoryQueueDescriptorConfiguration configuration)
    {
        var queueName = configuration.Name!;

        if (HasEndpoint(configuration.Features.Get<ReceiveFaultEndpointFeature>()))
        {
            throw ThrowHelper.FaultOrSkippedQueueRequiresConsumingEndpoint("fault", queueName);
        }

        if (HasEndpoint(configuration.Features.Get<ReceiveSkippedEndpointFeature>()))
        {
            throw ThrowHelper.FaultOrSkippedQueueRequiresConsumingEndpoint("skipped", queueName);
        }
    }

    private static bool HasEndpoint(ReceiveFaultEndpointFeature? feature)
        => feature is { IsDisabled: false, Address: not null }
            or { IsDisabled: false, QueueName: not null };

    private static bool HasEndpoint(ReceiveSkippedEndpointFeature? feature)
        => feature is { IsDisabled: false, Address: not null }
            or { IsDisabled: false, QueueName: not null };

    private static void CopyFaultEndpointFeature(
        InMemoryQueueDescriptorConfiguration configuration,
        InMemoryReceiveEndpointConfiguration target)
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
        InMemoryQueueDescriptorConfiguration configuration,
        InMemoryReceiveEndpointConfiguration target)
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

    /// <summary>
    /// Factory method that creates a new <see cref="InMemoryMessagingTransportDescriptor"/>.
    /// </summary>
    /// <param name="discoveryContext">The messaging setup context used to discover handlers and routes.</param>
    /// <returns>A new transport descriptor instance.</returns>
    public static InMemoryMessagingTransportDescriptor New(IMessagingSetupContext discoveryContext)
        => new(discoveryContext);
}
