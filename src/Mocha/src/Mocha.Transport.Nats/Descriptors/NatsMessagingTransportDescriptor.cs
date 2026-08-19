namespace Mocha.Transport.Nats;

/// <summary>
/// Default implementation of <see cref="INatsMessagingTransportDescriptor"/>.
/// </summary>
public sealed class NatsMessagingTransportDescriptor
    : MessagingTransportDescriptor<NatsTransportConfiguration>
    , INatsMessagingTransportDescriptor
{
    private readonly List<NatsStreamTopologyDescriptor> _streams = [];
    private readonly List<NatsConsumerTopologyDescriptor> _consumers = [];
    private readonly List<NatsReceiveEndpointDescriptor> _receiveEndpoints = [];

    /// <summary>
    /// Creates a descriptor bound to the specified setup context.
    /// </summary>
    /// <param name="setupContext">The setup context for the current bus configuration.</param>
    public NatsMessagingTransportDescriptor(IMessagingSetupContext setupContext)
        : base(setupContext)
    {
        Configuration = new NatsTransportConfiguration();
    }

    /// <inheritdoc />
    protected internal override NatsTransportConfiguration Configuration { get; protected set; }

    /// <summary>
    /// Builds the transport configuration from every declaration made on this descriptor.
    /// </summary>
    /// <returns>The transport configuration.</returns>
    public NatsTransportConfiguration CreateConfiguration()
    {
        Configuration.Streams = [.. _streams.Select(s => s.CreateConfiguration())];
        Configuration.Consumers = [.. _consumers.Select(c => c.CreateConfiguration())];
        Configuration.ReceiveEndpoints =
        [
            .. _receiveEndpoints.Select(ReceiveEndpointConfiguration (e) => e.CreateConfiguration())
        ];

        return Configuration;
    }

    /// <inheritdoc />
    public INatsReceiveEndpointDescriptor Endpoint(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var endpoint = _receiveEndpoints.FirstOrDefault(e => e.CreateConfiguration().Name == name);

        if (endpoint is null)
        {
            endpoint = NatsReceiveEndpointDescriptor.New(Context, name);
            _receiveEndpoints.Add(endpoint);
        }

        return endpoint;
    }

    /// <inheritdoc />
    public IMessagingTransportHandlerDescriptor<INatsReceiveEndpointDescriptor> Handler<THandler>()
        where THandler : class, IHandler
    {
        var name = Context.Naming.GetReceiveEndpointName(typeof(THandler), ReceiveEndpointKind.Default);
        var endpoint = Endpoint(name);

        endpoint.Handler(typeof(THandler));

        return new MessagingTransportHandlerDescriptor<INatsReceiveEndpointDescriptor>(endpoint);
    }

    /// <inheritdoc />
    public IMessagingTransportConsumerDescriptor<INatsReceiveEndpointDescriptor> Consumer<TConsumer>()
        where TConsumer : class, IConsumer
    {
        var name = Context.Naming.GetReceiveEndpointName(typeof(TConsumer), ReceiveEndpointKind.Default);
        var endpoint = Endpoint(name);

        endpoint.Consumer(typeof(TConsumer));

        return new MessagingTransportConsumerDescriptor<INatsReceiveEndpointDescriptor>(endpoint);
    }

    /// <inheritdoc />
    public INatsStreamTopologyDescriptor DeclareStream(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var stream = _streams.FirstOrDefault(s => s.CreateConfiguration().Name == name);

        if (stream is null)
        {
            stream = NatsStreamTopologyDescriptor.New(Context, name);
            _streams.Add(stream);
        }

        return stream;
    }

    /// <inheritdoc />
    public INatsConsumerTopologyDescriptor DeclareConsumer(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var consumer = _consumers.FirstOrDefault(c => c.CreateConfiguration().Name == name);

        if (consumer is null)
        {
            consumer = NatsConsumerTopologyDescriptor.New(Context, name);
            _consumers.Add(consumer);
        }

        return consumer;
    }

    /// <inheritdoc />
    public INatsMessagingTransportDescriptor ConnectionProvider(
        Func<IServiceProvider, INatsConnectionProvider> connectionProvider)
    {
        ArgumentNullException.ThrowIfNull(connectionProvider);

        Configuration.ConnectionProvider = connectionProvider;
        return this;
    }

    /// <inheritdoc />
    public INatsMessagingTransportDescriptor StreamName(string streamName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamName);

        Configuration.StreamName = streamName;
        return this;
    }

    /// <inheritdoc />
    public INatsMessagingTransportDescriptor EnableScheduling(bool enable = true)
    {
        Configuration.EnableScheduling = enable;
        return this;
    }

    /// <inheritdoc />
    public INatsMessagingTransportDescriptor EnablePublishDeduplication(bool enable = true)
    {
        Configuration.EnablePublishDeduplication = enable;
        return this;
    }

    /// <inheritdoc />
    public INatsMessagingTransportDescriptor AutoProvision(bool autoProvision = true)
    {
        Configuration.AutoProvision = autoProvision;
        return this;
    }

    /// <inheritdoc />
    public INatsMessagingTransportDescriptor ConfigureDefaults(Action<NatsBusDefaults> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        configure(Configuration.Defaults);

        return this;
    }

    /// <inheritdoc />
    public new INatsMessagingTransportDescriptor ModifyOptions(Action<TransportOptions> configure)
    {
        base.ModifyOptions(configure);
        return this;
    }

    /// <inheritdoc />
    public new INatsMessagingTransportDescriptor Schema(string schema)
    {
        base.Schema(schema);
        return this;
    }

    /// <inheritdoc />
    public new INatsMessagingTransportDescriptor Name(string name)
    {
        base.Name(name);
        return this;
    }

    /// <inheritdoc />
    public new INatsMessagingTransportDescriptor BindImplicitly()
    {
        base.BindImplicitly();
        return this;
    }

    /// <inheritdoc />
    public new INatsMessagingTransportDescriptor BindExplicitly()
    {
        base.BindExplicitly();
        return this;
    }

    /// <inheritdoc />
    public new INatsMessagingTransportDescriptor AddConvention(IConvention convention)
    {
        base.AddConvention(convention);
        return this;
    }

    /// <inheritdoc />
    public new INatsMessagingTransportDescriptor IsDefaultTransport()
    {
        base.IsDefaultTransport();
        return this;
    }

    /// <inheritdoc />
    public new INatsMessagingTransportDescriptor UseRoutingStrategy(Func<IServiceProvider, RoutingStrategy> factory)
    {
        base.UseRoutingStrategy(factory);
        return this;
    }

    /// <inheritdoc />
    public new INatsMessagingTransportDescriptor UseDispatch(
        DispatchMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null)
    {
        base.UseDispatch(configuration, before, after);
        return this;
    }

    /// <inheritdoc />
    public new INatsMessagingTransportDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null)
    {
        base.UseReceive(configuration, before, after);
        return this;
    }
}
