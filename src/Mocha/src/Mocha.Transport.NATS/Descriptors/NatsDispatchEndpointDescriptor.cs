namespace Mocha.Transport.NATS;

/// <summary>
/// Descriptor for configuring a NATS dispatch endpoint that targets a subject for outbound message delivery.
/// </summary>
internal sealed class NatsDispatchEndpointDescriptor
    : DispatchEndpointDescriptor<NatsDispatchEndpointConfiguration>
    , INatsDispatchEndpointDescriptor
{
    private NatsDispatchEndpointDescriptor(IMessagingConfigurationContext context, string name) : base(context)
    {
        Configuration = new NatsDispatchEndpointConfiguration { Name = name, SubjectName = name };
    }

    protected internal override NatsDispatchEndpointConfiguration Configuration { get; protected set; }

    /// <inheritdoc />
    public INatsDispatchEndpointDescriptor ToSubject(string name)
    {
        Configuration.SubjectName = name;
        return this;
    }

    /// <inheritdoc />
    public new INatsDispatchEndpointDescriptor Send<TMessage>()
    {
        base.Send<TMessage>();
        return this;
    }

    /// <inheritdoc />
    public new INatsDispatchEndpointDescriptor Publish<TMessage>()
    {
        base.Publish<TMessage>();
        return this;
    }

    /// <inheritdoc />
    public new INatsDispatchEndpointDescriptor UseDispatch(DispatchMiddlewareConfiguration configuration)
    {
        base.UseDispatch(configuration);
        return this;
    }

    /// <inheritdoc />
    public new INatsDispatchEndpointDescriptor AppendDispatch(
        string after,
        DispatchMiddlewareConfiguration configuration)
    {
        base.AppendDispatch(after, configuration);
        return this;
    }

    /// <inheritdoc />
    public new INatsDispatchEndpointDescriptor PrependDispatch(
        string before,
        DispatchMiddlewareConfiguration configuration)
    {
        base.PrependDispatch(before, configuration);
        return this;
    }

    /// <summary>
    /// Builds the final dispatch endpoint configuration from the accumulated settings.
    /// </summary>
    /// <returns>The configured <see cref="NatsDispatchEndpointConfiguration"/>.</returns>
    public NatsDispatchEndpointConfiguration CreateConfiguration() => Configuration;

    /// <summary>
    /// Creates a new dispatch endpoint descriptor with the specified name, defaulting to a subject destination.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="name">The endpoint name, also used as the default subject name.</param>
    /// <returns>A new dispatch endpoint descriptor.</returns>
    public static NatsDispatchEndpointDescriptor New(IMessagingConfigurationContext context, string name)
        => new(context, name);
}
