using Mocha.Features;

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
    private readonly List<RabbitMQExchangeTopologyDescriptor> _exchanges = [];
    private readonly List<RabbitMQQueueDescriptor> _queues = [];
    private readonly List<RabbitMQQueueTopologyDescriptor> _queueTopology = [];
    private readonly List<RabbitMQBindingTopologyDescriptor> _bindings = [];

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
    public new IRabbitMQMessagingTransportDescriptor UseRoutingStrategy(Func<IServiceProvider, RoutingStrategy> factory)
    {
        base.UseRoutingStrategy(factory);

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
        var endpoint = _receiveEndpoints.FirstOrDefault(e => e.Configuration.Name.EqualsOrdinal(name));

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
        var endpoint = _dispatchEndpoints.FirstOrDefault(e => e.Configuration.Name.EqualsOrdinal(name));
        if (endpoint is null)
        {
            endpoint = RabbitMQDispatchEndpointDescriptor.New(Context, name);
            _dispatchEndpoints.Add(endpoint);
        }

        return endpoint;
    }

    /// <inheritdoc />
    public IRabbitMQExchangeTopologyDescriptor DeclareExchange(string name)
    {
        var exchange = _exchanges.FirstOrDefault(e => e.Configuration.Name.EqualsOrdinal(name));
        if (exchange is null)
        {
            exchange = RabbitMQExchangeTopologyDescriptor.New(Context, name);
            _exchanges.Add(exchange);
        }
        return exchange;
    }

    /// <inheritdoc />
    public IRabbitMQQueueTopologyDescriptor DeclareQueue(string name)
    {
        var queue = _queueTopology.FirstOrDefault(q => q.Configuration.Name.EqualsOrdinal(name));
        if (queue is null)
        {
            queue = RabbitMQQueueTopologyDescriptor.New(Context, name);
            _queueTopology.Add(queue);
        }
        return queue;
    }

    /// <inheritdoc />
    public IRabbitMQBindingTopologyDescriptor DeclareBinding(string exchange, string queue)
    {
        var binding = RabbitMQBindingTopologyDescriptor.New(Context, exchange, queue);
        _bindings.Add(binding);
        return binding;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor Queue(string name)
    {
        var queue = _queues.FirstOrDefault(q => q.Configuration.Name.EqualsOrdinal(name));
        if (queue is not null)
        {
            return queue;
        }

        queue = RabbitMQQueueDescriptor.New(Context, name);
        _queues.Add(queue);
        return queue;
    }

    /// <summary>
    /// Builds the final transport configuration from all accumulated descriptor settings, including receive and dispatch endpoints.
    /// </summary>
    /// <returns>A fully populated <see cref="RabbitMQTransportConfiguration"/> ready for transport initialization.</returns>
    public RabbitMQTransportConfiguration CreateConfiguration()
    {
        foreach (var queue in _queues.Select(q => q.CreateConfiguration()))
        {
            ConfigureQueueTopology(queue);
            ConfigureQueueEndpoint(queue);
        }

        var exchanges = _exchanges.Select(e => e.CreateConfiguration()).ToList();
        var queues = _queueTopology.Select(q => q.CreateConfiguration()).ToList();
        var bindings = _bindings.Select(b => b.CreateConfiguration()).ToList();

        var receiveEndpoints = _receiveEndpoints.Select(e => e.CreateConfiguration()).ToList();

        Configuration.Exchanges = exchanges;
        Configuration.Queues = queues;
        Configuration.Bindings = bindings;

        Configuration.ReceiveEndpoints = receiveEndpoints
            .Select(ReceiveEndpointConfiguration (e) => e)
            .ToList();

        Configuration.DispatchEndpoints = _dispatchEndpoints
            .Select(DispatchEndpointConfiguration (e) => e.CreateConfiguration())
            .ToList();

        return Configuration;
    }

    private void ConfigureQueueTopology(RabbitMQQueueDescriptorConfiguration configuration)
    {
        var queue = DeclareQueue(configuration.Name!);
        ApplyQueueConfiguration(configuration.Queue, queue);

        var schema = Configuration.Schema ?? RabbitMQTransportConfiguration.DefaultSchema;
        foreach (var binding in configuration.SourceBindings)
        {
            if (!RabbitMQDestinations.TryResolveSourceExchange(schema, binding.Source, out var exchangeName))
            {
                throw new InvalidOperationException(
                    $"BindFrom source '{binding.Source}' could not be resolved to a RabbitMQ exchange name.");
            }

            DeclareExchange(exchangeName);
            var descriptor = DeclareBinding(exchangeName, configuration.Name!);
            if (binding.RoutingKey is not null)
            {
                descriptor.RoutingKey(binding.RoutingKey);
            }
        }
    }

    private void ConfigureQueueEndpoint(RabbitMQQueueDescriptorConfiguration configuration)
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

        if (configuration.MaxPrefetch is not null)
        {
            target.MaxPrefetch = configuration.MaxPrefetch.Value;
        }

        target.ReceiveMiddlewares.AddRange(configuration.ReceiveMiddlewares);
        target.ReceivePipelineModifiers.AddRange(configuration.ReceivePipelineModifiers);
        CopyFaultEndpointFeature(configuration, target);
        CopySkippedEndpointFeature(configuration, target);
    }

    private static void CopyFaultEndpointFeature(
        RabbitMQQueueDescriptorConfiguration configuration,
        RabbitMQReceiveEndpointConfiguration target)
    {
        var source = configuration.Features.Get<ReceiveFaultEndpointFeature>();
        if (source is null)
        {
            return;
        }

        var targetFeature = target.Features.GetOrSet<ReceiveFaultEndpointFeature>();
        if (targetFeature is { Address: not null } or { IsDisabled: true })
        {
            return;
        }

        targetFeature.Address = source.Address;
        targetFeature.IsDisabled = source.IsDisabled;
    }

    private static void CopySkippedEndpointFeature(
        RabbitMQQueueDescriptorConfiguration configuration,
        RabbitMQReceiveEndpointConfiguration target)
    {
        var source = configuration.Features.Get<ReceiveSkippedEndpointFeature>();
        if (source is null)
        {
            return;
        }

        var targetFeature = target.Features.GetOrSet<ReceiveSkippedEndpointFeature>();
        if (targetFeature is { Address: not null } or { IsDisabled: true })
        {
            return;
        }

        targetFeature.Address = source.Address;
        targetFeature.IsDisabled = source.IsDisabled;
    }

    private static void ApplyQueueConfiguration(
        RabbitMQQueueConfiguration configuration,
        IRabbitMQQueueTopologyDescriptor descriptor)
    {
        if (configuration.Durable is { } durable)
        {
            descriptor.Durable(durable);
        }

        if (configuration.Exclusive is { } exclusive)
        {
            descriptor.Exclusive(exclusive);
        }

        if (configuration.AutoDelete is { } autoDelete)
        {
            descriptor.AutoDelete(autoDelete);
        }

        if (configuration.Arguments is not null)
        {
            foreach (var (key, value) in configuration.Arguments)
            {
                descriptor.WithArgument(key, value);
            }
        }

        if (configuration.AutoProvision is { } autoProvision)
        {
            descriptor.AutoProvision(autoProvision);
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
