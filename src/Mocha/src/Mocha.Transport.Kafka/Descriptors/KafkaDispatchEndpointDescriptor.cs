namespace Mocha.Transport.Kafka;

/// <summary>
/// Descriptor for configuring a Kafka dispatch endpoint that targets a topic for outbound message delivery.
/// </summary>
internal sealed class KafkaDispatchEndpointDescriptor
    : DispatchEndpointDescriptor<KafkaDispatchEndpointConfiguration>
    , IKafkaDispatchEndpointDescriptor
{
    private KafkaDispatchEndpointDescriptor(IMessagingConfigurationContext context, string name) : base(context)
    {
        Configuration = new KafkaDispatchEndpointConfiguration { Name = name, TopicName = name };
    }

    protected internal override KafkaDispatchEndpointConfiguration Configuration { get; protected set; }

    /// <inheritdoc />
    public IKafkaDispatchEndpointDescriptor ToTopic(string name)
    {
        Configuration.TopicName = name;
        return this;
    }

    /// <inheritdoc />
    public new IKafkaDispatchEndpointDescriptor Send<TMessage>()
    {
        base.Send<TMessage>();
        return this;
    }

    /// <inheritdoc />
    public new IKafkaDispatchEndpointDescriptor Publish<TMessage>()
    {
        base.Publish<TMessage>();
        return this;
    }

    /// <inheritdoc />
    public new IKafkaDispatchEndpointDescriptor UseDispatch(
        DispatchMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null)
    {
        base.UseDispatch(configuration, before, after);
        return this;
    }

    /// <summary>
    /// Builds the final dispatch endpoint configuration from the accumulated settings.
    /// </summary>
    /// <returns>The configured <see cref="KafkaDispatchEndpointConfiguration"/>.</returns>
    public KafkaDispatchEndpointConfiguration CreateConfiguration() => Configuration;

    /// <summary>
    /// Creates a new dispatch endpoint descriptor with the specified name, defaulting to a topic destination.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="name">The endpoint name, also used as the default topic name.</param>
    /// <returns>A new dispatch endpoint descriptor.</returns>
    public static KafkaDispatchEndpointDescriptor New(IMessagingConfigurationContext context, string name)
        => new(context, name);
}
