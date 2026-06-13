namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Adapter that presents a <see cref="RabbitMQReceiveEndpointDescriptor"/> through the unified
/// queue front-door interface. The queue identity is pinned at construction; all configuration
/// methods delegate to the backing descriptor.
/// </summary>
internal sealed class RabbitMQQueueEndpointDescriptor : IRabbitMQQueueEndpointDescriptor
{
    private readonly RabbitMQReceiveEndpointDescriptor _inner;

    internal RabbitMQQueueEndpointDescriptor(RabbitMQReceiveEndpointDescriptor inner)
    {
        _inner = inner;
        _inner.PinQueueIdentity();
    }

    /// <summary>
    /// Gets the backing receive endpoint descriptor.
    /// </summary>
    internal RabbitMQReceiveEndpointDescriptor Inner => _inner;

    /// <inheritdoc />
    public IRabbitMQQueueEndpointDescriptor Handler<THandler>() where THandler : class, IHandler
    {
        _inner.Handler<THandler>();

        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueEndpointDescriptor Handler(Type handlerType)
    {
        _inner.Handler(handlerType);

        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueEndpointDescriptor Consumer(Type consumerType)
    {
        _inner.Consumer(consumerType);

        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueEndpointDescriptor Consumer<TConsumer>() where TConsumer : class, IConsumer
    {
        _inner.Consumer<TConsumer>();

        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueEndpointDescriptor Receives<TMessage>()
    {
        _inner.Receives<TMessage>();

        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueEndpointDescriptor Receives<TMessage>(Action<IReceiveTypeBindDescriptor> configure)
    {
        _inner.Receives<TMessage>(configure);

        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueEndpointDescriptor Receives(Type messageType)
    {
        _inner.Receives(messageType);

        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueEndpointDescriptor AutoBind(bool enabled)
    {
        _inner.AutoBind(enabled);

        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueEndpointDescriptor BindFrom(Uri source, string? routingKey = null)
    {
        _inner.BindFrom(source, routingKey);

        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueEndpointDescriptor Kind(ReceiveEndpointKind kind)
    {
        _inner.Kind(kind);

        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueEndpointDescriptor ErrorQueue(string name)
    {
        _inner.ErrorQueue(name);

        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueEndpointDescriptor DisableErrorQueue()
    {
        _inner.DisableErrorQueue();

        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueEndpointDescriptor SkippedQueue(string name)
    {
        _inner.SkippedQueue(name);

        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueEndpointDescriptor DisableSkippedQueue()
    {
        _inner.DisableSkippedQueue();

        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueEndpointDescriptor MaxConcurrency(int maxConcurrency)
    {
        _inner.MaxConcurrency(maxConcurrency);

        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueEndpointDescriptor FaultEndpoint(string name)
    {
        _inner.FaultEndpoint(name);

        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueEndpointDescriptor SkippedEndpoint(string name)
    {
        _inner.SkippedEndpoint(name);

        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueEndpointDescriptor MaxPrefetch(ushort maxPrefetch)
    {
        _inner.MaxPrefetch(maxPrefetch);

        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueEndpointDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null)
    {
        _inner.UseReceive(configuration, before, after);

        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueEndpointDescriptor Durable(bool durable = true)
    {
        _inner.Configuration.QueueDurable = durable;

        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueEndpointDescriptor Quorum()
        => WithArgument("x-queue-type", RabbitMQQueueType.Quorum);

    /// <inheritdoc />
    public IRabbitMQQueueEndpointDescriptor WithArgument(string key, object value)
    {
        _inner.Configuration.QueueArguments ??= new Dictionary<string, object>();
        _inner.Configuration.QueueArguments[key] = value;

        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueEndpointDescriptor AutoProvision(bool autoProvision = true)
    {
        _inner.Configuration.QueueAutoProvision = autoProvision;

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
    public IRabbitMQQueueEndpointDescriptor Queue(string name)
        => throw ThrowHelper.QueueIdentityPinned(_inner.Configuration.QueueName ?? _inner.Configuration.Name ?? string.Empty);

    // Explicit interface implementations guard all base-cast and reflection bypass paths.
    // IRabbitMQReceiveEndpointDescriptor covariant overrides:

    IRabbitMQReceiveEndpointDescriptor IRabbitMQReceiveEndpointDescriptor.Handler<THandler>()
        => Handler<THandler>();

    IRabbitMQReceiveEndpointDescriptor IRabbitMQReceiveEndpointDescriptor.Handler(Type handlerType)
        => Handler(handlerType);

    IRabbitMQReceiveEndpointDescriptor IRabbitMQReceiveEndpointDescriptor.Consumer(Type consumerType)
        => Consumer(consumerType);

    IRabbitMQReceiveEndpointDescriptor IRabbitMQReceiveEndpointDescriptor.Consumer<TConsumer>()
        => Consumer<TConsumer>();

    IRabbitMQReceiveEndpointDescriptor IRabbitMQReceiveEndpointDescriptor.Receives<TMessage>()
        => Receives<TMessage>();

    IRabbitMQReceiveEndpointDescriptor IRabbitMQReceiveEndpointDescriptor.Receives<TMessage>(
        Action<IReceiveTypeBindDescriptor> configure)
        => Receives<TMessage>(configure);

    IRabbitMQReceiveEndpointDescriptor IRabbitMQReceiveEndpointDescriptor.Receives(Type messageType)
        => Receives(messageType);

    IRabbitMQReceiveEndpointDescriptor IRabbitMQReceiveEndpointDescriptor.AutoBind(bool enabled)
        => AutoBind(enabled);

    IRabbitMQReceiveEndpointDescriptor IRabbitMQReceiveEndpointDescriptor.BindFrom(Uri source, string? routingKey)
        => BindFrom(source, routingKey);

    IRabbitMQReceiveEndpointDescriptor IRabbitMQReceiveEndpointDescriptor.Kind(ReceiveEndpointKind kind)
        => Kind(kind);

    IRabbitMQReceiveEndpointDescriptor IRabbitMQReceiveEndpointDescriptor.ErrorQueue(string name)
        => ErrorQueue(name);

    IRabbitMQReceiveEndpointDescriptor IRabbitMQReceiveEndpointDescriptor.DisableErrorQueue()
        => DisableErrorQueue();

    IRabbitMQReceiveEndpointDescriptor IRabbitMQReceiveEndpointDescriptor.SkippedQueue(string name)
        => SkippedQueue(name);

    IRabbitMQReceiveEndpointDescriptor IRabbitMQReceiveEndpointDescriptor.DisableSkippedQueue()
        => DisableSkippedQueue();

    IRabbitMQReceiveEndpointDescriptor IRabbitMQReceiveEndpointDescriptor.MaxConcurrency(int maxConcurrency)
        => MaxConcurrency(maxConcurrency);

    IRabbitMQReceiveEndpointDescriptor IRabbitMQReceiveEndpointDescriptor.FaultEndpoint(string name)
        => FaultEndpoint(name);

    IRabbitMQReceiveEndpointDescriptor IRabbitMQReceiveEndpointDescriptor.SkippedEndpoint(string name)
        => SkippedEndpoint(name);

    IRabbitMQReceiveEndpointDescriptor IRabbitMQReceiveEndpointDescriptor.MaxPrefetch(ushort maxPrefetch)
        => MaxPrefetch(maxPrefetch);

    IRabbitMQReceiveEndpointDescriptor IRabbitMQReceiveEndpointDescriptor.UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before,
        string? after)
        => UseReceive(configuration, before, after);

    /// <summary>
    /// Explicit guard for the base-cast rename path. Queue identity is fixed at construction
    /// and cannot be changed through an <see cref="IRabbitMQReceiveEndpointDescriptor"/> reference.
    /// </summary>
    /// <exception cref="InvalidOperationException">Always thrown.</exception>
    IRabbitMQReceiveEndpointDescriptor IRabbitMQReceiveEndpointDescriptor.Queue(string name)
        => throw ThrowHelper.QueueIdentityPinned(_inner.Configuration.QueueName ?? _inner.Configuration.Name ?? string.Empty);

    // IReceiveEndpointDescriptor<RabbitMQReceiveEndpointConfiguration> base implementations:

    IReceiveEndpointDescriptor<RabbitMQReceiveEndpointConfiguration>
        IReceiveEndpointDescriptor<RabbitMQReceiveEndpointConfiguration>.Handler<THandler>()
        => Handler<THandler>();

    IReceiveEndpointDescriptor<RabbitMQReceiveEndpointConfiguration>
        IReceiveEndpointDescriptor<RabbitMQReceiveEndpointConfiguration>.Handler(Type handlerType)
        => Handler(handlerType);

    IReceiveEndpointDescriptor<RabbitMQReceiveEndpointConfiguration>
        IReceiveEndpointDescriptor<RabbitMQReceiveEndpointConfiguration>.Consumer(Type consumerType)
        => Consumer(consumerType);

    IReceiveEndpointDescriptor<RabbitMQReceiveEndpointConfiguration>
        IReceiveEndpointDescriptor<RabbitMQReceiveEndpointConfiguration>.Consumer<TConsumer>()
        => Consumer<TConsumer>();

    IReceiveEndpointDescriptor<RabbitMQReceiveEndpointConfiguration>
        IReceiveEndpointDescriptor<RabbitMQReceiveEndpointConfiguration>.Receives<TMessage>()
        => Receives<TMessage>();

    IReceiveEndpointDescriptor<RabbitMQReceiveEndpointConfiguration>
        IReceiveEndpointDescriptor<RabbitMQReceiveEndpointConfiguration>.Receives<TMessage>(
            Action<IReceiveTypeBindDescriptor> configure)
        => Receives<TMessage>(configure);

    IReceiveEndpointDescriptor<RabbitMQReceiveEndpointConfiguration>
        IReceiveEndpointDescriptor<RabbitMQReceiveEndpointConfiguration>.Receives(Type messageType)
        => Receives(messageType);

    IReceiveEndpointDescriptor<RabbitMQReceiveEndpointConfiguration>
        IReceiveEndpointDescriptor<RabbitMQReceiveEndpointConfiguration>.AutoBind(bool enabled)
        => AutoBind(enabled);

    IReceiveEndpointDescriptor<RabbitMQReceiveEndpointConfiguration>
        IReceiveEndpointDescriptor<RabbitMQReceiveEndpointConfiguration>.BindFrom(Uri source, string? routingKey)
        => BindFrom(source, routingKey);

    IReceiveEndpointDescriptor<RabbitMQReceiveEndpointConfiguration>
        IReceiveEndpointDescriptor<RabbitMQReceiveEndpointConfiguration>.Kind(ReceiveEndpointKind kind)
        => Kind(kind);

    IReceiveEndpointDescriptor<RabbitMQReceiveEndpointConfiguration>
        IReceiveEndpointDescriptor<RabbitMQReceiveEndpointConfiguration>.MaxConcurrency(int maxConcurrency)
        => MaxConcurrency(maxConcurrency);

    IReceiveEndpointDescriptor<RabbitMQReceiveEndpointConfiguration>
        IReceiveEndpointDescriptor<RabbitMQReceiveEndpointConfiguration>.FaultEndpoint(string name)
        => FaultEndpoint(name);

    IReceiveEndpointDescriptor<RabbitMQReceiveEndpointConfiguration>
        IReceiveEndpointDescriptor<RabbitMQReceiveEndpointConfiguration>.SkippedEndpoint(string name)
        => SkippedEndpoint(name);

    IReceiveEndpointDescriptor<RabbitMQReceiveEndpointConfiguration>
        IReceiveEndpointDescriptor<RabbitMQReceiveEndpointConfiguration>.UseReceive(
            ReceiveMiddlewareConfiguration configuration,
            string? before,
            string? after)
        => UseReceive(configuration, before, after);

    // IMessagingDescriptor<RabbitMQReceiveEndpointConfiguration> base implementations:

    IMessagingDescriptorExtension<RabbitMQReceiveEndpointConfiguration>
        IMessagingDescriptor<RabbitMQReceiveEndpointConfiguration>.Extend()
        => _inner.Extend();

    IMessagingDescriptorExtension<RabbitMQReceiveEndpointConfiguration>
        IMessagingDescriptor<RabbitMQReceiveEndpointConfiguration>.ExtendWith(
            Action<IMessagingDescriptorExtension<RabbitMQReceiveEndpointConfiguration>> configure)
        => _inner.ExtendWith(configure);

    IMessagingDescriptorExtension<RabbitMQReceiveEndpointConfiguration>
        IMessagingDescriptor<RabbitMQReceiveEndpointConfiguration>.ExtendWith<TState>(
            Action<IMessagingDescriptorExtension<RabbitMQReceiveEndpointConfiguration>, TState> configure,
            TState state)
        => _inner.ExtendWith(configure, state);

    IMessagingDescriptorExtension IMessagingDescriptor.Extend()
        => _inner.Extend();
}
