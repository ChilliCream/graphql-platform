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
    public new IRabbitMQMessagingTransportDescriptor UseDispatch(DispatchMiddlewareConfiguration configuration)
    {
        base.UseDispatch(configuration);

        return this;
    }

    /// <inheritdoc />
    public new IRabbitMQMessagingTransportDescriptor AppendDispatch(
        string after,
        DispatchMiddlewareConfiguration configuration)
    {
        base.AppendDispatch(after, configuration);

        return this;
    }

    /// <inheritdoc />
    public new IRabbitMQMessagingTransportDescriptor PrependDispatch(
        string before,
        DispatchMiddlewareConfiguration configuration)
    {
        base.PrependDispatch(before, configuration);

        return this;
    }

    /// <inheritdoc />
    public new IRabbitMQMessagingTransportDescriptor UseReceive(ReceiveMiddlewareConfiguration configuration)
    {
        base.UseReceive(configuration);

        return this;
    }

    /// <inheritdoc />
    public new IRabbitMQMessagingTransportDescriptor AppendReceive(
        string after,
        ReceiveMiddlewareConfiguration configuration)
    {
        base.AppendReceive(after, configuration);

        return this;
    }

    /// <inheritdoc />
    public new IRabbitMQMessagingTransportDescriptor PrependReceive(
        string before,
        ReceiveMiddlewareConfiguration configuration)
    {
        base.PrependReceive(before, configuration);

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
        var binding = _bindings.FirstOrDefault(b =>
            b.Extend().Configuration.Source.EqualsOrdinal(exchange)
            && b.Extend().Configuration.Destination.EqualsOrdinal(queue)
        );

        if (binding is null)
        {
            binding = RabbitMQBindingDescriptor.New(Context, exchange, queue);
            _bindings.Add(binding);
        }

        return binding;
    }

    /// <summary>
    /// Builds the final transport configuration from all accumulated descriptor settings, including receive and dispatch endpoints.
    /// </summary>
    /// <returns>A fully populated <see cref="RabbitMQTransportConfiguration"/> ready for transport initialization.</returns>
    public RabbitMQTransportConfiguration CreateConfiguration()
    {
        Configuration.ReceiveEndpoints = _receiveEndpoints
            .Select(ReceiveEndpointConfiguration (e) => e.CreateConfiguration())
            .ToList();

        Configuration.DispatchEndpoints = _dispatchEndpoints
            .Select(DispatchEndpointConfiguration (e) => e.CreateConfiguration())
            .ToList();

        Configuration.Exchanges = _exchanges.Select(e => e.CreateConfiguration()).ToList();

        Configuration.Queues = _queues.Select(q => q.CreateConfiguration()).ToList();

        Configuration.Bindings = _bindings.Select(b => b.CreateConfiguration()).ToList();

        return Configuration;
    }

    /// <summary>
    /// Creates a new <see cref="RabbitMQMessagingTransportDescriptor"/> for the given setup context.
    /// </summary>
    /// <param name="discoveryContext">The messaging setup context.</param>
    /// <returns>A new transport descriptor instance.</returns>
    public static RabbitMQMessagingTransportDescriptor New(IMessagingSetupContext discoveryContext)
        => new(discoveryContext);
}
