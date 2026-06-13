namespace Mocha.Transport.Postgres;

/// <summary>
/// Adapter that presents a <see cref="PostgresReceiveEndpointDescriptor"/> through the unified
/// queue front-door interface. The queue identity is pinned at construction; all configuration
/// methods delegate to the backing descriptor.
/// </summary>
internal sealed class PostgresQueueEndpointDescriptor : IPostgresQueueEndpointDescriptor
{
    private readonly PostgresReceiveEndpointDescriptor _inner;

    internal PostgresQueueEndpointDescriptor(PostgresReceiveEndpointDescriptor inner)
    {
        _inner = inner;
        _inner.PinQueueIdentity();
    }

    /// <summary>
    /// Gets the backing receive endpoint descriptor.
    /// </summary>
    internal PostgresReceiveEndpointDescriptor Inner => _inner;

    /// <inheritdoc />
    public IPostgresQueueEndpointDescriptor Handler<THandler>() where THandler : class, IHandler
    {
        _inner.Handler<THandler>();

        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueEndpointDescriptor Handler(Type handlerType)
    {
        _inner.Handler(handlerType);

        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueEndpointDescriptor Consumer(Type consumerType)
    {
        _inner.Consumer(consumerType);

        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueEndpointDescriptor Consumer<TConsumer>() where TConsumer : class, IConsumer
    {
        _inner.Consumer<TConsumer>();

        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueEndpointDescriptor Receives<TMessage>()
    {
        _inner.Receives<TMessage>();

        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueEndpointDescriptor Receives<TMessage>(Action<IReceiveTypeBindDescriptor> configure)
    {
        _inner.Receives<TMessage>(configure);

        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueEndpointDescriptor Receives(Type messageType)
    {
        _inner.Receives(messageType);

        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueEndpointDescriptor AutoBind(bool enabled)
    {
        _inner.AutoBind(enabled);

        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueEndpointDescriptor BindFrom(Uri source, string? routingKey = null)
    {
        _inner.BindFrom(source, routingKey);

        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueEndpointDescriptor Kind(ReceiveEndpointKind kind)
    {
        _inner.Kind(kind);

        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueEndpointDescriptor ErrorQueue(string name)
    {
        _inner.ErrorQueue(name);

        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueEndpointDescriptor DisableErrorQueue()
    {
        _inner.DisableErrorQueue();

        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueEndpointDescriptor SkippedQueue(string name)
    {
        _inner.SkippedQueue(name);

        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueEndpointDescriptor DisableSkippedQueue()
    {
        _inner.DisableSkippedQueue();

        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueEndpointDescriptor MaxConcurrency(int maxConcurrency)
    {
        _inner.MaxConcurrency(maxConcurrency);

        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueEndpointDescriptor FaultEndpoint(string name)
    {
        _inner.FaultEndpoint(name);

        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueEndpointDescriptor SkippedEndpoint(string name)
    {
        _inner.SkippedEndpoint(name);

        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueEndpointDescriptor MaxBatchSize(int size)
    {
        _inner.MaxBatchSize(size);

        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueEndpointDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null)
    {
        _inner.UseReceive(configuration, before, after);

        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueEndpointDescriptor AutoProvision(bool autoProvision = true)
    {
        _inner.Configuration.AutoProvision = autoProvision;

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
    public IPostgresQueueEndpointDescriptor Queue(string name)
        => throw ThrowHelper.QueueIdentityPinned(_inner.Configuration.QueueName ?? _inner.Configuration.Name ?? string.Empty);

    // Explicit interface implementations guard all base-cast and reflection bypass paths.
    // IPostgresReceiveEndpointDescriptor covariant overrides:

    IPostgresReceiveEndpointDescriptor IPostgresReceiveEndpointDescriptor.Handler<THandler>()
        => Handler<THandler>();

    IPostgresReceiveEndpointDescriptor IPostgresReceiveEndpointDescriptor.Handler(Type handlerType)
        => Handler(handlerType);

    IPostgresReceiveEndpointDescriptor IPostgresReceiveEndpointDescriptor.Consumer(Type consumerType)
        => Consumer(consumerType);

    IPostgresReceiveEndpointDescriptor IPostgresReceiveEndpointDescriptor.Consumer<TConsumer>()
        => Consumer<TConsumer>();

    IPostgresReceiveEndpointDescriptor IPostgresReceiveEndpointDescriptor.Receives<TMessage>()
        => Receives<TMessage>();

    IPostgresReceiveEndpointDescriptor IPostgresReceiveEndpointDescriptor.Receives<TMessage>(
        Action<IReceiveTypeBindDescriptor> configure)
        => Receives<TMessage>(configure);

    IPostgresReceiveEndpointDescriptor IPostgresReceiveEndpointDescriptor.Receives(Type messageType)
        => Receives(messageType);

    IPostgresReceiveEndpointDescriptor IPostgresReceiveEndpointDescriptor.AutoBind(bool enabled)
        => AutoBind(enabled);

    IPostgresReceiveEndpointDescriptor IPostgresReceiveEndpointDescriptor.BindFrom(Uri source, string? routingKey)
        => BindFrom(source, routingKey);

    IPostgresReceiveEndpointDescriptor IPostgresReceiveEndpointDescriptor.Kind(ReceiveEndpointKind kind)
        => Kind(kind);

    IPostgresReceiveEndpointDescriptor IPostgresReceiveEndpointDescriptor.ErrorQueue(string name)
        => ErrorQueue(name);

    IPostgresReceiveEndpointDescriptor IPostgresReceiveEndpointDescriptor.DisableErrorQueue()
        => DisableErrorQueue();

    IPostgresReceiveEndpointDescriptor IPostgresReceiveEndpointDescriptor.SkippedQueue(string name)
        => SkippedQueue(name);

    IPostgresReceiveEndpointDescriptor IPostgresReceiveEndpointDescriptor.DisableSkippedQueue()
        => DisableSkippedQueue();

    IPostgresReceiveEndpointDescriptor IPostgresReceiveEndpointDescriptor.MaxConcurrency(int maxConcurrency)
        => MaxConcurrency(maxConcurrency);

    IPostgresReceiveEndpointDescriptor IPostgresReceiveEndpointDescriptor.FaultEndpoint(string name)
        => FaultEndpoint(name);

    IPostgresReceiveEndpointDescriptor IPostgresReceiveEndpointDescriptor.SkippedEndpoint(string name)
        => SkippedEndpoint(name);

    IPostgresReceiveEndpointDescriptor IPostgresReceiveEndpointDescriptor.MaxBatchSize(int size)
        => MaxBatchSize(size);

    IPostgresReceiveEndpointDescriptor IPostgresReceiveEndpointDescriptor.UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before,
        string? after)
        => UseReceive(configuration, before, after);

    /// <summary>
    /// Explicit guard for the base-cast rename path. Queue identity is fixed at construction
    /// and cannot be changed through an <see cref="IPostgresReceiveEndpointDescriptor"/> reference.
    /// </summary>
    /// <exception cref="InvalidOperationException">Always thrown.</exception>
    IPostgresReceiveEndpointDescriptor IPostgresReceiveEndpointDescriptor.Queue(string name)
        => throw ThrowHelper.QueueIdentityPinned(_inner.Configuration.QueueName ?? _inner.Configuration.Name ?? string.Empty);

    // IReceiveEndpointDescriptor<PostgresReceiveEndpointConfiguration> base implementations:

    IReceiveEndpointDescriptor<PostgresReceiveEndpointConfiguration>
        IReceiveEndpointDescriptor<PostgresReceiveEndpointConfiguration>.Handler<THandler>()
        => Handler<THandler>();

    IReceiveEndpointDescriptor<PostgresReceiveEndpointConfiguration>
        IReceiveEndpointDescriptor<PostgresReceiveEndpointConfiguration>.Handler(Type handlerType)
        => Handler(handlerType);

    IReceiveEndpointDescriptor<PostgresReceiveEndpointConfiguration>
        IReceiveEndpointDescriptor<PostgresReceiveEndpointConfiguration>.Consumer(Type consumerType)
        => Consumer(consumerType);

    IReceiveEndpointDescriptor<PostgresReceiveEndpointConfiguration>
        IReceiveEndpointDescriptor<PostgresReceiveEndpointConfiguration>.Consumer<TConsumer>()
        => Consumer<TConsumer>();

    IReceiveEndpointDescriptor<PostgresReceiveEndpointConfiguration>
        IReceiveEndpointDescriptor<PostgresReceiveEndpointConfiguration>.Receives<TMessage>()
        => Receives<TMessage>();

    IReceiveEndpointDescriptor<PostgresReceiveEndpointConfiguration>
        IReceiveEndpointDescriptor<PostgresReceiveEndpointConfiguration>.Receives<TMessage>(
            Action<IReceiveTypeBindDescriptor> configure)
        => Receives<TMessage>(configure);

    IReceiveEndpointDescriptor<PostgresReceiveEndpointConfiguration>
        IReceiveEndpointDescriptor<PostgresReceiveEndpointConfiguration>.Receives(Type messageType)
        => Receives(messageType);

    IReceiveEndpointDescriptor<PostgresReceiveEndpointConfiguration>
        IReceiveEndpointDescriptor<PostgresReceiveEndpointConfiguration>.AutoBind(bool enabled)
        => AutoBind(enabled);

    IReceiveEndpointDescriptor<PostgresReceiveEndpointConfiguration>
        IReceiveEndpointDescriptor<PostgresReceiveEndpointConfiguration>.BindFrom(Uri source, string? routingKey)
        => BindFrom(source, routingKey);

    IReceiveEndpointDescriptor<PostgresReceiveEndpointConfiguration>
        IReceiveEndpointDescriptor<PostgresReceiveEndpointConfiguration>.Kind(ReceiveEndpointKind kind)
        => Kind(kind);

    IReceiveEndpointDescriptor<PostgresReceiveEndpointConfiguration>
        IReceiveEndpointDescriptor<PostgresReceiveEndpointConfiguration>.MaxConcurrency(int maxConcurrency)
        => MaxConcurrency(maxConcurrency);

    IReceiveEndpointDescriptor<PostgresReceiveEndpointConfiguration>
        IReceiveEndpointDescriptor<PostgresReceiveEndpointConfiguration>.FaultEndpoint(string name)
        => FaultEndpoint(name);

    IReceiveEndpointDescriptor<PostgresReceiveEndpointConfiguration>
        IReceiveEndpointDescriptor<PostgresReceiveEndpointConfiguration>.SkippedEndpoint(string name)
        => SkippedEndpoint(name);

    IReceiveEndpointDescriptor<PostgresReceiveEndpointConfiguration>
        IReceiveEndpointDescriptor<PostgresReceiveEndpointConfiguration>.UseReceive(
            ReceiveMiddlewareConfiguration configuration,
            string? before,
            string? after)
        => UseReceive(configuration, before, after);

    // IMessagingDescriptor<PostgresReceiveEndpointConfiguration> base implementations:

    IMessagingDescriptorExtension<PostgresReceiveEndpointConfiguration>
        IMessagingDescriptor<PostgresReceiveEndpointConfiguration>.Extend()
        => _inner.Extend();

    IMessagingDescriptorExtension<PostgresReceiveEndpointConfiguration>
        IMessagingDescriptor<PostgresReceiveEndpointConfiguration>.ExtendWith(
            Action<IMessagingDescriptorExtension<PostgresReceiveEndpointConfiguration>> configure)
        => _inner.ExtendWith(configure);

    IMessagingDescriptorExtension<PostgresReceiveEndpointConfiguration>
        IMessagingDescriptor<PostgresReceiveEndpointConfiguration>.ExtendWith<TState>(
            Action<IMessagingDescriptorExtension<PostgresReceiveEndpointConfiguration>, TState> configure,
            TState state)
        => _inner.ExtendWith(configure, state);

    IMessagingDescriptorExtension IMessagingDescriptor.Extend()
        => _inner.Extend();
}
