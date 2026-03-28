namespace Mocha.Transport.NATS;

/// <summary>
/// Descriptor implementation for configuring a NATS JetStream durable consumer.
/// </summary>
internal sealed class NatsConsumerDescriptor
    : MessagingDescriptorBase<NatsConsumerConfiguration>
    , INatsConsumerDescriptor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NatsConsumerDescriptor"/> class.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="name">The initial consumer name.</param>
    public NatsConsumerDescriptor(IMessagingConfigurationContext context, string name) : base(context)
    {
        Configuration = new NatsConsumerConfiguration { Name = name };
    }

    /// <inheritdoc />
    protected internal override NatsConsumerConfiguration Configuration { get; protected set; }

    /// <inheritdoc />
    public INatsConsumerDescriptor Name(string name)
    {
        Configuration.Name = name;
        return this;
    }

    /// <inheritdoc />
    public INatsConsumerDescriptor Stream(string streamName)
    {
        Configuration.StreamName = streamName;
        return this;
    }

    /// <inheritdoc />
    public INatsConsumerDescriptor FilterSubject(string filterSubject)
    {
        Configuration.FilterSubject = filterSubject;
        return this;
    }

    /// <inheritdoc />
    public INatsConsumerDescriptor MaxAckPending(int maxAckPending)
    {
        Configuration.MaxAckPending = maxAckPending;
        return this;
    }

    /// <inheritdoc />
    public INatsConsumerDescriptor AckWait(TimeSpan ackWait)
    {
        Configuration.AckWait = ackWait;
        return this;
    }

    /// <inheritdoc />
    public INatsConsumerDescriptor MaxDeliver(int maxDeliver)
    {
        Configuration.MaxDeliver = maxDeliver;
        return this;
    }

    /// <inheritdoc />
    public INatsConsumerDescriptor AutoProvision(bool autoProvision = true)
    {
        Configuration.AutoProvision = autoProvision;
        return this;
    }

    /// <summary>
    /// Creates the final consumer configuration.
    /// </summary>
    /// <returns>The configured consumer configuration.</returns>
    public NatsConsumerConfiguration CreateConfiguration() => Configuration;

    /// <summary>
    /// Creates a new consumer descriptor with the specified name.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="name">The consumer name.</param>
    /// <returns>A new consumer descriptor.</returns>
    public static NatsConsumerDescriptor New(IMessagingConfigurationContext context, string name)
        => new(context, name);
}
