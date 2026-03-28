using NATS.Client.Core;

namespace Mocha.Transport.NATS;

/// <summary>
/// Fluent descriptor for configuring a NATS JetStream messaging transport, including endpoints, topology, and connection settings.
/// </summary>
public sealed class NatsMessagingTransportDescriptor
    : MessagingTransportDescriptor<NatsTransportConfiguration>
    , INatsMessagingTransportDescriptor
{
    private readonly List<NatsReceiveEndpointDescriptor> _receiveEndpoints = [];
    private readonly List<NatsDispatchEndpointDescriptor> _dispatchEndpoints = [];
    private readonly List<NatsStreamDescriptor> _streams = [];
    private readonly List<NatsConsumerDescriptor> _consumers = [];

    /// <summary>
    /// Creates a new NATS transport descriptor bound to the given setup context.
    /// </summary>
    /// <param name="discoveryContext">The messaging setup context used for handler and route discovery.</param>
    public NatsMessagingTransportDescriptor(IMessagingSetupContext discoveryContext) : base(discoveryContext)
    {
        Configuration = new NatsTransportConfiguration();
    }

    protected internal override NatsTransportConfiguration Configuration { get; protected set; }

    /// <inheritdoc />
    public new INatsMessagingTransportDescriptor ModifyOptions(Action<TransportOptions> configure)
    {
        base.ModifyOptions(configure);

        return this;
    }

    /// <inheritdoc />
    public new INatsMessagingTransportDescriptor Name(string name)
    {
        base.Name(name);

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
    public new INatsMessagingTransportDescriptor UseDispatch(DispatchMiddlewareConfiguration configuration)
    {
        base.UseDispatch(configuration);

        return this;
    }

    /// <inheritdoc />
    public new INatsMessagingTransportDescriptor AppendDispatch(
        string after,
        DispatchMiddlewareConfiguration configuration)
    {
        base.AppendDispatch(after, configuration);

        return this;
    }

    /// <inheritdoc />
    public new INatsMessagingTransportDescriptor PrependDispatch(
        string before,
        DispatchMiddlewareConfiguration configuration)
    {
        base.PrependDispatch(before, configuration);

        return this;
    }

    /// <inheritdoc />
    public new INatsMessagingTransportDescriptor UseReceive(ReceiveMiddlewareConfiguration configuration)
    {
        base.UseReceive(configuration);

        return this;
    }

    /// <inheritdoc />
    public new INatsMessagingTransportDescriptor AppendReceive(
        string after,
        ReceiveMiddlewareConfiguration configuration)
    {
        base.AppendReceive(after, configuration);

        return this;
    }

    /// <inheritdoc />
    public new INatsMessagingTransportDescriptor PrependReceive(
        string before,
        ReceiveMiddlewareConfiguration configuration)
    {
        base.PrependReceive(before, configuration);

        return this;
    }

    /// <inheritdoc />
    public new INatsMessagingTransportDescriptor Schema(string schema)
    {
        base.Schema(schema);

        return this;
    }

    /// <inheritdoc />
    public new INatsMessagingTransportDescriptor BindHandlersImplicitly()
    {
        base.BindHandlersImplicitly();

        return this;
    }

    /// <inheritdoc />
    public new INatsMessagingTransportDescriptor BindHandlersExplicitly()
    {
        base.BindHandlersExplicitly();

        return this;
    }

    /// <inheritdoc />
    public INatsMessagingTransportDescriptor AutoProvision(bool autoProvision = true)
    {
        Configuration.AutoProvision = autoProvision;
        return this;
    }

    /// <inheritdoc />
    public INatsMessagingTransportDescriptor Url(string url)
    {
        Configuration.Url = url;

        return this;
    }

    /// <inheritdoc />
    public INatsMessagingTransportDescriptor ConnectionFactory(
        Func<IServiceProvider, NatsConnection> connectionFactory)
    {
        Configuration.ConnectionFactory = connectionFactory;

        return this;
    }

    /// <inheritdoc />
    public INatsMessagingTransportDescriptor ConfigureDefaults(Action<NatsBusDefaults> configure)
    {
        configure(Configuration.Defaults);

        return this;
    }

    /// <inheritdoc />
    public INatsReceiveEndpointDescriptor Endpoint(string name)
    {
        var endpoint = _receiveEndpoints.FirstOrDefault(e =>
            e.Extend().Configuration.Name.EqualsOrdinal(name)
            || e.Extend().Configuration.SubjectName.EqualsOrdinal(name)
        );

        if (endpoint is null)
        {
            endpoint = NatsReceiveEndpointDescriptor.New(Context, name);
            _receiveEndpoints.Add(endpoint);
        }

        return endpoint;
    }

    /// <inheritdoc />
    public INatsDispatchEndpointDescriptor DispatchEndpoint(string name)
    {
        var endpoint = _dispatchEndpoints.FirstOrDefault(e => e.Extend().Configuration.Name.EqualsOrdinal(name));
        if (endpoint is null)
        {
            endpoint = NatsDispatchEndpointDescriptor.New(Context, name);
            _dispatchEndpoints.Add(endpoint);
        }

        return endpoint;
    }

    /// <inheritdoc />
    public INatsStreamDescriptor DeclareStream(string name)
    {
        var stream = _streams.FirstOrDefault(s => s.Extend().Configuration.Name.EqualsOrdinal(name));
        if (stream is null)
        {
            stream = NatsStreamDescriptor.New(Context, name);
            _streams.Add(stream);
        }

        return stream;
    }

    /// <inheritdoc />
    public INatsConsumerDescriptor DeclareConsumer(string name)
    {
        var consumer = _consumers.FirstOrDefault(c => c.Extend().Configuration.Name.EqualsOrdinal(name));
        if (consumer is null)
        {
            consumer = NatsConsumerDescriptor.New(Context, name);
            _consumers.Add(consumer);
        }

        return consumer;
    }

    /// <summary>
    /// Builds the final transport configuration from all accumulated descriptor settings, including receive and dispatch endpoints.
    /// </summary>
    /// <returns>A fully populated <see cref="NatsTransportConfiguration"/> ready for transport initialization.</returns>
    public NatsTransportConfiguration CreateConfiguration()
    {
        Configuration.ReceiveEndpoints = _receiveEndpoints
            .Select(ReceiveEndpointConfiguration (e) => e.CreateConfiguration())
            .ToList();

        Configuration.DispatchEndpoints = _dispatchEndpoints
            .Select(DispatchEndpointConfiguration (e) => e.CreateConfiguration())
            .ToList();

        Configuration.Streams = _streams.Select(s => s.CreateConfiguration()).ToList();
        Configuration.Consumers = _consumers.Select(c => c.CreateConfiguration()).ToList();

        return Configuration;
    }

    /// <summary>
    /// Creates a new <see cref="NatsMessagingTransportDescriptor"/> for the given setup context.
    /// </summary>
    /// <param name="discoveryContext">The messaging setup context.</param>
    /// <returns>A new transport descriptor instance.</returns>
    public static NatsMessagingTransportDescriptor New(IMessagingSetupContext discoveryContext)
        => new(discoveryContext);
}
