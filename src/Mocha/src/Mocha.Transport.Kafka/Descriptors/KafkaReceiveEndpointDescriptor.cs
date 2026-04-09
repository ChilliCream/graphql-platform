namespace Mocha.Transport.Kafka;

/// <summary>
/// Descriptor for configuring a Kafka receive endpoint that consumes messages from a specific topic.
/// </summary>
internal sealed class KafkaReceiveEndpointDescriptor
    : ReceiveEndpointDescriptor<KafkaReceiveEndpointConfiguration>
    , IKafkaReceiveEndpointDescriptor
{
    private KafkaReceiveEndpointDescriptor(IMessagingConfigurationContext discoveryContext, string name)
        : base(discoveryContext)
    {
        Configuration = new KafkaReceiveEndpointConfiguration { Name = name, TopicName = name, ConsumerGroupId = name };
    }

    /// <inheritdoc />
    public new IKafkaReceiveEndpointDescriptor Handler<THandler>() where THandler : class, IHandler
    {
        base.Handler<THandler>();

        return this;
    }

    /// <inheritdoc />
    public new IKafkaReceiveEndpointDescriptor Consumer<TConsumer>() where TConsumer : class, IConsumer
    {
        base.Consumer<TConsumer>();

        return this;
    }

    /// <inheritdoc />
    public new IKafkaReceiveEndpointDescriptor Kind(ReceiveEndpointKind kind)
    {
        base.Kind(kind);

        return this;
    }

    /// <inheritdoc />
    public new IKafkaReceiveEndpointDescriptor MaxConcurrency(int maxConcurrency)
    {
        base.MaxConcurrency(maxConcurrency);

        return this;
    }

    /// <inheritdoc />
    public IKafkaReceiveEndpointDescriptor Topic(string name)
    {
        Configuration.TopicName = name;

        return this;
    }

    /// <inheritdoc />
    public IKafkaReceiveEndpointDescriptor ConsumerGroup(string groupId)
    {
        Configuration.ConsumerGroupId = groupId;

        return this;
    }

    /// <inheritdoc />
    public new IKafkaReceiveEndpointDescriptor FaultEndpoint(string name)
    {
        base.FaultEndpoint(name);

        return this;
    }

    /// <inheritdoc />
    public new IKafkaReceiveEndpointDescriptor SkippedEndpoint(string name)
    {
        base.SkippedEndpoint(name);

        return this;
    }

    /// <inheritdoc />
    public new IKafkaReceiveEndpointDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null)
    {
        base.UseReceive(configuration, before, after);

        return this;
    }

    /// <summary>
    /// Builds the final receive endpoint configuration from the accumulated settings.
    /// </summary>
    /// <returns>The configured <see cref="KafkaReceiveEndpointConfiguration"/>.</returns>
    public KafkaReceiveEndpointConfiguration CreateConfiguration()
    {
        return Configuration;
    }

    /// <summary>
    /// Creates a new receive endpoint descriptor with the specified name, which also serves as the default topic and consumer group name.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="name">The endpoint name, default topic name, and default consumer group ID.</param>
    /// <returns>A new receive endpoint descriptor.</returns>
    public static KafkaReceiveEndpointDescriptor New(IMessagingConfigurationContext context, string name)
        => new(context, name);
}
