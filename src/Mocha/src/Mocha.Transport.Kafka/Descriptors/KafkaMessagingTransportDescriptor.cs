using Confluent.Kafka;

namespace Mocha.Transport.Kafka;

/// <summary>
/// Fluent descriptor for configuring a Kafka messaging transport, including endpoints, topology, and connection settings.
/// </summary>
public sealed class KafkaMessagingTransportDescriptor
    : MessagingTransportDescriptor<KafkaTransportConfiguration>
    , IKafkaMessagingTransportDescriptor
{
    private readonly List<KafkaReceiveEndpointDescriptor> _receiveEndpoints = [];
    private readonly List<KafkaDispatchEndpointDescriptor> _dispatchEndpoints = [];
    private readonly List<KafkaTopicDescriptor> _topics = [];

    /// <summary>
    /// Creates a new Kafka transport descriptor bound to the given setup context.
    /// </summary>
    /// <param name="discoveryContext">The messaging setup context used for handler and route discovery.</param>
    public KafkaMessagingTransportDescriptor(IMessagingSetupContext discoveryContext) : base(discoveryContext)
    {
        Configuration = new KafkaTransportConfiguration();
    }

    protected internal override KafkaTransportConfiguration Configuration { get; protected set; }

    /// <inheritdoc />
    public new IKafkaMessagingTransportDescriptor ModifyOptions(Action<TransportOptions> configure)
    {
        base.ModifyOptions(configure);

        return this;
    }

    /// <inheritdoc />
    public new IKafkaMessagingTransportDescriptor Name(string name)
    {
        base.Name(name);

        return this;
    }

    /// <inheritdoc />
    public new IKafkaMessagingTransportDescriptor AddConvention(IConvention convention)
    {
        base.AddConvention(convention);

        return this;
    }

    /// <inheritdoc />
    public new IKafkaMessagingTransportDescriptor IsDefaultTransport()
    {
        base.IsDefaultTransport();

        return this;
    }

    /// <inheritdoc />
    public new IKafkaMessagingTransportDescriptor UseDispatch(
        DispatchMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null)
    {
        base.UseDispatch(configuration, before, after);

        return this;
    }

    /// <inheritdoc />
    public new IKafkaMessagingTransportDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null)
    {
        base.UseReceive(configuration, before, after);

        return this;
    }

    /// <inheritdoc />
    public new IKafkaMessagingTransportDescriptor Schema(string schema)
    {
        base.Schema(schema);

        return this;
    }

    /// <inheritdoc />
    public new IKafkaMessagingTransportDescriptor BindHandlersImplicitly()
    {
        base.BindHandlersImplicitly();

        return this;
    }

    /// <inheritdoc />
    public new IKafkaMessagingTransportDescriptor BindHandlersExplicitly()
    {
        base.BindHandlersExplicitly();

        return this;
    }

    /// <inheritdoc />
    public IKafkaMessagingTransportDescriptor AutoProvision(bool autoProvision = true)
    {
        Configuration.AutoProvision = autoProvision;
        return this;
    }

    /// <inheritdoc />
    public IKafkaMessagingTransportDescriptor BootstrapServers(string bootstrapServers)
    {
        Configuration.BootstrapServers = bootstrapServers;
        return this;
    }

    /// <inheritdoc />
    public IKafkaMessagingTransportDescriptor ConfigureProducer(Action<ProducerConfig> configure)
    {
        var existing = Configuration.ProducerConfigOverrides;
        Configuration.ProducerConfigOverrides = existing is null
            ? configure
            : c => { existing(c); configure(c); };
        return this;
    }

    /// <inheritdoc />
    public IKafkaMessagingTransportDescriptor ConfigureConsumer(Action<ConsumerConfig> configure)
    {
        var existing = Configuration.ConsumerConfigOverrides;
        Configuration.ConsumerConfigOverrides = existing is null
            ? configure
            : c => { existing(c); configure(c); };
        return this;
    }

    /// <inheritdoc />
    public IKafkaMessagingTransportDescriptor ConfigureDefaults(Action<KafkaBusDefaults> configure)
    {
        configure(Configuration.Defaults);

        return this;
    }

    /// <inheritdoc />
    public IKafkaReceiveEndpointDescriptor Endpoint(string name)
    {
        var endpoint = _receiveEndpoints.FirstOrDefault(e =>
            e.Extend().Configuration.Name.EqualsOrdinal(name) || e.Extend().Configuration.TopicName.EqualsOrdinal(name)
        );

        if (endpoint is null)
        {
            endpoint = KafkaReceiveEndpointDescriptor.New(Context, name);
            _receiveEndpoints.Add(endpoint);
        }

        return endpoint;
    }

    /// <inheritdoc />
    public IKafkaDispatchEndpointDescriptor DispatchEndpoint(string name)
    {
        var endpoint = _dispatchEndpoints.FirstOrDefault(e => e.Extend().Configuration.Name.EqualsOrdinal(name));
        if (endpoint is null)
        {
            endpoint = KafkaDispatchEndpointDescriptor.New(Context, name);
            _dispatchEndpoints.Add(endpoint);
        }

        return endpoint;
    }

    /// <inheritdoc />
    public IKafkaTopicDescriptor DeclareTopic(string name)
    {
        var topic = _topics.FirstOrDefault(t => t.Extend().Configuration.Name.EqualsOrdinal(name));
        if (topic is null)
        {
            topic = KafkaTopicDescriptor.New(Context, name);
            _topics.Add(topic);
        }

        return topic;
    }

    /// <summary>
    /// Builds the final transport configuration from all accumulated descriptor settings, including receive and dispatch endpoints.
    /// </summary>
    /// <returns>A fully populated <see cref="KafkaTransportConfiguration"/> ready for transport initialization.</returns>
    public KafkaTransportConfiguration CreateConfiguration()
    {
        Configuration.ReceiveEndpoints = _receiveEndpoints
            .Select(ReceiveEndpointConfiguration (e) => e.CreateConfiguration())
            .ToList();

        Configuration.DispatchEndpoints = _dispatchEndpoints
            .Select(DispatchEndpointConfiguration (e) => e.CreateConfiguration())
            .ToList();

        Configuration.Topics = _topics.Select(t => t.CreateConfiguration()).ToList();

        return Configuration;
    }

    /// <summary>
    /// Creates a new <see cref="KafkaMessagingTransportDescriptor"/> for the given setup context.
    /// </summary>
    /// <param name="discoveryContext">The messaging setup context.</param>
    /// <returns>A new transport descriptor instance.</returns>
    public static KafkaMessagingTransportDescriptor New(IMessagingSetupContext discoveryContext)
        => new(discoveryContext);
}
