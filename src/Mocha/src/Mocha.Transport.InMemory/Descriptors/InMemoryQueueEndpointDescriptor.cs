namespace Mocha.Transport.InMemory;

/// <summary>
/// Adapter that presents an <see cref="InMemoryReceiveEndpointDescriptor"/> through the unified
/// queue front-door interface. The queue identity is pinned at construction; all configuration
/// methods delegate to the backing descriptor.
/// </summary>
internal sealed class InMemoryQueueEndpointDescriptor : IInMemoryQueueEndpointDescriptor
{
    private readonly InMemoryReceiveEndpointDescriptor _inner;

    internal InMemoryQueueEndpointDescriptor(InMemoryReceiveEndpointDescriptor inner)
    {
        _inner = inner;
        _inner.PinQueueIdentity();
    }

    /// <summary>
    /// Gets the backing receive endpoint descriptor.
    /// </summary>
    internal InMemoryReceiveEndpointDescriptor Inner => _inner;

    /// <inheritdoc />
    public IInMemoryQueueEndpointDescriptor Handler<THandler>() where THandler : class, IHandler
    {
        _inner.Handler<THandler>();

        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueEndpointDescriptor Handler(Type handlerType)
    {
        _inner.Handler(handlerType);

        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueEndpointDescriptor Consumer(Type consumerType)
    {
        _inner.Consumer(consumerType);

        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueEndpointDescriptor Consumer<TConsumer>() where TConsumer : class, IConsumer
    {
        _inner.Consumer<TConsumer>();

        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueEndpointDescriptor Receives<TMessage>()
    {
        _inner.Receives<TMessage>();

        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueEndpointDescriptor Receives<TMessage>(Action<IReceiveTypeBindDescriptor> configure)
    {
        _inner.Receives<TMessage>(configure);

        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueEndpointDescriptor Receives(Type messageType)
    {
        _inner.Receives(messageType);

        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueEndpointDescriptor AutoBind(bool enabled)
    {
        _inner.AutoBind(enabled);

        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueEndpointDescriptor BindFrom(Uri source, string? routingKey = null)
    {
        _inner.BindFrom(source, routingKey);

        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueEndpointDescriptor Kind(ReceiveEndpointKind kind)
    {
        _inner.Kind(kind);

        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueEndpointDescriptor FaultEndpoint(string name)
    {
        _inner.FaultEndpoint(name);

        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueEndpointDescriptor SkippedEndpoint(string name)
    {
        _inner.SkippedEndpoint(name);

        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueEndpointDescriptor MaxConcurrency(int maxConcurrency)
    {
        _inner.MaxConcurrency(maxConcurrency);

        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueEndpointDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null)
    {
        _inner.UseReceive(configuration, before, after);

        return this;
    }

    /// <summary>
    /// Not supported on a unified queue endpoint. The queue name is fixed at construction.
    /// </summary>
    /// <exception cref="InvalidOperationException">Always thrown.</exception>
    [Obsolete(
        "Queue identity is fixed on a unified queue endpoint. "
        + "The queue name cannot be changed after creation. "
        + "Use t.Queue(name, q => ...) to configure the endpoint with a specific queue name.",
        error: true)]
    public IInMemoryQueueEndpointDescriptor Queue(string name)
        => throw ThrowHelper.QueueIdentityPinned(_inner.Configuration.QueueName ?? _inner.Configuration.Name ?? string.Empty);

    // Explicit interface implementations guard all base-cast and reflection bypass paths.
    // IInMemoryReceiveEndpointDescriptor covariant overrides:

    IInMemoryReceiveEndpointDescriptor IInMemoryReceiveEndpointDescriptor.Handler<THandler>()
        => Handler<THandler>();

    IInMemoryReceiveEndpointDescriptor IInMemoryReceiveEndpointDescriptor.Handler(Type handlerType)
        => Handler(handlerType);

    IInMemoryReceiveEndpointDescriptor IInMemoryReceiveEndpointDescriptor.Consumer(Type consumerType)
        => Consumer(consumerType);

    IInMemoryReceiveEndpointDescriptor IInMemoryReceiveEndpointDescriptor.Consumer<TConsumer>()
        => Consumer<TConsumer>();

    IInMemoryReceiveEndpointDescriptor IInMemoryReceiveEndpointDescriptor.Receives<TMessage>()
        => Receives<TMessage>();

    IInMemoryReceiveEndpointDescriptor IInMemoryReceiveEndpointDescriptor.Receives<TMessage>(
        Action<IReceiveTypeBindDescriptor> configure)
        => Receives<TMessage>(configure);

    IInMemoryReceiveEndpointDescriptor IInMemoryReceiveEndpointDescriptor.Receives(Type messageType)
        => Receives(messageType);

    IInMemoryReceiveEndpointDescriptor IInMemoryReceiveEndpointDescriptor.AutoBind(bool enabled)
        => AutoBind(enabled);

    IInMemoryReceiveEndpointDescriptor IInMemoryReceiveEndpointDescriptor.BindFrom(Uri source, string? routingKey)
        => BindFrom(source, routingKey);

    IInMemoryReceiveEndpointDescriptor IInMemoryReceiveEndpointDescriptor.Kind(ReceiveEndpointKind kind)
        => Kind(kind);

    IInMemoryReceiveEndpointDescriptor IInMemoryReceiveEndpointDescriptor.FaultEndpoint(string name)
        => FaultEndpoint(name);

    IInMemoryReceiveEndpointDescriptor IInMemoryReceiveEndpointDescriptor.SkippedEndpoint(string name)
        => SkippedEndpoint(name);

    IInMemoryReceiveEndpointDescriptor IInMemoryReceiveEndpointDescriptor.MaxConcurrency(int maxConcurrency)
        => MaxConcurrency(maxConcurrency);

    IInMemoryReceiveEndpointDescriptor IInMemoryReceiveEndpointDescriptor.UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before,
        string? after)
        => UseReceive(configuration, before, after);

    /// <summary>
    /// Explicit guard for the base-cast rename path. Queue identity is fixed at construction
    /// and cannot be changed through an <see cref="IInMemoryReceiveEndpointDescriptor"/> reference.
    /// </summary>
    /// <exception cref="InvalidOperationException">Always thrown.</exception>
    IInMemoryReceiveEndpointDescriptor IInMemoryReceiveEndpointDescriptor.Queue(string name)
        => throw ThrowHelper.QueueIdentityPinned(_inner.Configuration.QueueName ?? _inner.Configuration.Name ?? string.Empty);

    // IReceiveEndpointDescriptor<InMemoryReceiveEndpointConfiguration> base implementations:

    IReceiveEndpointDescriptor<InMemoryReceiveEndpointConfiguration>
        IReceiveEndpointDescriptor<InMemoryReceiveEndpointConfiguration>.Handler<THandler>()
        => Handler<THandler>();

    IReceiveEndpointDescriptor<InMemoryReceiveEndpointConfiguration>
        IReceiveEndpointDescriptor<InMemoryReceiveEndpointConfiguration>.Handler(Type handlerType)
        => Handler(handlerType);

    IReceiveEndpointDescriptor<InMemoryReceiveEndpointConfiguration>
        IReceiveEndpointDescriptor<InMemoryReceiveEndpointConfiguration>.Consumer(Type consumerType)
        => Consumer(consumerType);

    IReceiveEndpointDescriptor<InMemoryReceiveEndpointConfiguration>
        IReceiveEndpointDescriptor<InMemoryReceiveEndpointConfiguration>.Consumer<TConsumer>()
        => Consumer<TConsumer>();

    IReceiveEndpointDescriptor<InMemoryReceiveEndpointConfiguration>
        IReceiveEndpointDescriptor<InMemoryReceiveEndpointConfiguration>.Receives<TMessage>()
        => Receives<TMessage>();

    IReceiveEndpointDescriptor<InMemoryReceiveEndpointConfiguration>
        IReceiveEndpointDescriptor<InMemoryReceiveEndpointConfiguration>.Receives<TMessage>(
            Action<IReceiveTypeBindDescriptor> configure)
        => Receives<TMessage>(configure);

    IReceiveEndpointDescriptor<InMemoryReceiveEndpointConfiguration>
        IReceiveEndpointDescriptor<InMemoryReceiveEndpointConfiguration>.Receives(Type messageType)
        => Receives(messageType);

    IReceiveEndpointDescriptor<InMemoryReceiveEndpointConfiguration>
        IReceiveEndpointDescriptor<InMemoryReceiveEndpointConfiguration>.AutoBind(bool enabled)
        => AutoBind(enabled);

    IReceiveEndpointDescriptor<InMemoryReceiveEndpointConfiguration>
        IReceiveEndpointDescriptor<InMemoryReceiveEndpointConfiguration>.BindFrom(Uri source, string? routingKey)
        => BindFrom(source, routingKey);

    IReceiveEndpointDescriptor<InMemoryReceiveEndpointConfiguration>
        IReceiveEndpointDescriptor<InMemoryReceiveEndpointConfiguration>.Kind(ReceiveEndpointKind kind)
        => Kind(kind);

    IReceiveEndpointDescriptor<InMemoryReceiveEndpointConfiguration>
        IReceiveEndpointDescriptor<InMemoryReceiveEndpointConfiguration>.MaxConcurrency(int maxConcurrency)
        => MaxConcurrency(maxConcurrency);

    IReceiveEndpointDescriptor<InMemoryReceiveEndpointConfiguration>
        IReceiveEndpointDescriptor<InMemoryReceiveEndpointConfiguration>.FaultEndpoint(string name)
        => FaultEndpoint(name);

    IReceiveEndpointDescriptor<InMemoryReceiveEndpointConfiguration>
        IReceiveEndpointDescriptor<InMemoryReceiveEndpointConfiguration>.SkippedEndpoint(string name)
        => SkippedEndpoint(name);

    IReceiveEndpointDescriptor<InMemoryReceiveEndpointConfiguration>
        IReceiveEndpointDescriptor<InMemoryReceiveEndpointConfiguration>.UseReceive(
            ReceiveMiddlewareConfiguration configuration,
            string? before,
            string? after)
        => UseReceive(configuration, before, after);

    // IMessagingDescriptor<InMemoryReceiveEndpointConfiguration> base implementations:

    IMessagingDescriptorExtension<InMemoryReceiveEndpointConfiguration>
        IMessagingDescriptor<InMemoryReceiveEndpointConfiguration>.Extend()
        => _inner.Extend();

    IMessagingDescriptorExtension<InMemoryReceiveEndpointConfiguration>
        IMessagingDescriptor<InMemoryReceiveEndpointConfiguration>.ExtendWith(
            Action<IMessagingDescriptorExtension<InMemoryReceiveEndpointConfiguration>> configure)
        => _inner.ExtendWith(configure);

    IMessagingDescriptorExtension<InMemoryReceiveEndpointConfiguration>
        IMessagingDescriptor<InMemoryReceiveEndpointConfiguration>.ExtendWith<TState>(
            Action<IMessagingDescriptorExtension<InMemoryReceiveEndpointConfiguration>, TState> configure,
            TState state)
        => _inner.ExtendWith(configure, state);

    IMessagingDescriptorExtension IMessagingDescriptor.Extend()
        => _inner.Extend();
}
