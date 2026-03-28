namespace Mocha.Transport.NATS;

/// <summary>
/// Descriptor implementation for configuring a NATS JetStream stream.
/// </summary>
internal sealed class NatsStreamDescriptor
    : MessagingDescriptorBase<NatsStreamConfiguration>
    , INatsStreamDescriptor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NatsStreamDescriptor"/> class.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="name">The initial stream name.</param>
    public NatsStreamDescriptor(IMessagingConfigurationContext context, string name) : base(context)
    {
        Configuration = new NatsStreamConfiguration { Name = name };
    }

    /// <inheritdoc />
    protected internal override NatsStreamConfiguration Configuration { get; protected set; }

    /// <inheritdoc />
    public INatsStreamDescriptor Name(string name)
    {
        Configuration.Name = name;
        return this;
    }

    /// <inheritdoc />
    public INatsStreamDescriptor Subject(string subject)
    {
        Configuration.Subjects.Add(subject);
        return this;
    }

    /// <inheritdoc />
    public INatsStreamDescriptor MaxMsgs(long maxMsgs)
    {
        Configuration.MaxMsgs = maxMsgs;
        return this;
    }

    /// <inheritdoc />
    public INatsStreamDescriptor MaxBytes(long maxBytes)
    {
        Configuration.MaxBytes = maxBytes;
        return this;
    }

    /// <inheritdoc />
    public INatsStreamDescriptor MaxAge(TimeSpan maxAge)
    {
        Configuration.MaxAge = maxAge;
        return this;
    }

    /// <inheritdoc />
    public INatsStreamDescriptor Replicas(int replicas)
    {
        Configuration.Replicas = replicas;
        return this;
    }

    /// <inheritdoc />
    public INatsStreamDescriptor AutoProvision(bool autoProvision = true)
    {
        Configuration.AutoProvision = autoProvision;
        return this;
    }

    /// <summary>
    /// Creates the final stream configuration.
    /// </summary>
    /// <returns>The configured stream configuration.</returns>
    public NatsStreamConfiguration CreateConfiguration() => Configuration;

    /// <summary>
    /// Creates a new stream descriptor with the specified name.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="name">The stream name.</param>
    /// <returns>A new stream descriptor.</returns>
    public static NatsStreamDescriptor New(IMessagingConfigurationContext context, string name)
        => new(context, name);
}
