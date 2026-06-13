namespace Mocha.Transport.InMemory;

internal sealed class InMemoryReceiveEndpointDescriptor
    : ReceiveEndpointDescriptor<InMemoryReceiveEndpointConfiguration>
    , IInMemoryReceiveEndpointDescriptor
{
    private bool _queueIdentityPinned;

    internal InMemoryReceiveEndpointDescriptor(IMessagingConfigurationContext discoveryContext, string name)
        : base(discoveryContext)
    {
        Configuration = new InMemoryReceiveEndpointConfiguration { Name = name, QueueName = name };
    }

    /// <summary>
    /// Gets a value indicating whether this descriptor's queue identity is pinned and cannot be renamed.
    /// A pinned descriptor was created via the unified <c>Queue(name, ...)</c> front door.
    /// </summary>
    internal bool IsQueueIdentityPinned => _queueIdentityPinned;

    /// <summary>
    /// Pins the queue identity so that subsequent calls to <see cref="Queue(string)"/> throw a build error.
    /// Called by the unified <c>Queue(name, ...)</c> front door adapter after the descriptor is created.
    /// </summary>
    internal void PinQueueIdentity()
    {
        _queueIdentityPinned = true;
    }

    public new IInMemoryReceiveEndpointDescriptor Handler<THandler>() where THandler : class, IHandler
    {
        base.Handler<THandler>();

        return this;
    }

    public new IInMemoryReceiveEndpointDescriptor Handler(Type handlerType)
    {
        base.Handler(handlerType);

        return this;
    }

    public new IInMemoryReceiveEndpointDescriptor Consumer(Type consumerType)
    {
        base.Consumer(consumerType);

        return this;
    }

    public new IInMemoryReceiveEndpointDescriptor Consumer<TConsumer>() where TConsumer : class, IConsumer
    {
        base.Consumer<TConsumer>();

        return this;
    }

    public new IInMemoryReceiveEndpointDescriptor Receives<TMessage>()
    {
        base.Receives<TMessage>();

        return this;
    }

    public new IInMemoryReceiveEndpointDescriptor Receives<TMessage>(Action<IReceiveTypeBindDescriptor> configure)
    {
        base.Receives<TMessage>(configure);

        return this;
    }

    public new IInMemoryReceiveEndpointDescriptor Receives(Type messageType)
    {
        base.Receives(messageType);

        return this;
    }

    public new IInMemoryReceiveEndpointDescriptor AutoBind(bool enabled)
    {
        base.AutoBind(enabled);

        return this;
    }

    public new IInMemoryReceiveEndpointDescriptor BindFrom(Uri source, string? routingKey = null)
    {
        base.BindFrom(source, routingKey);

        return this;
    }

    public new IInMemoryReceiveEndpointDescriptor Kind(ReceiveEndpointKind kind)
    {
        base.Kind(kind);

        return this;
    }

    public new IInMemoryReceiveEndpointDescriptor MaxConcurrency(int maxConcurrency)
    {
        base.MaxConcurrency(maxConcurrency);

        return this;
    }

    public IInMemoryReceiveEndpointDescriptor Queue(string name)
    {
        if (_queueIdentityPinned)
        {
            throw ThrowHelper.QueueIdentityPinned(Configuration.QueueName ?? Configuration.Name ?? string.Empty);
        }

        Configuration.QueueName = name;

        return this;
    }

    public new IInMemoryReceiveEndpointDescriptor FaultEndpoint(string name)
    {
        base.FaultEndpoint(name);

        return this;
    }

    public new IInMemoryReceiveEndpointDescriptor SkippedEndpoint(string name)
    {
        base.SkippedEndpoint(name);

        return this;
    }

    public new IInMemoryReceiveEndpointDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null)
    {
        base.UseReceive(configuration, before: before, after: after);

        return this;
    }

    public InMemoryReceiveEndpointConfiguration CreateConfiguration()
    {
        return Configuration;
    }

    public static InMemoryReceiveEndpointDescriptor New(IMessagingConfigurationContext context, string name)
        => new(context, name);
}
