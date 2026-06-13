namespace Mocha.Transport.Postgres;

internal sealed class PostgresReceiveEndpointDescriptor
    : ReceiveEndpointDescriptor<PostgresReceiveEndpointConfiguration>
    , IPostgresReceiveEndpointDescriptor
{
    private bool _queueIdentityPinned;

    internal PostgresReceiveEndpointDescriptor(IMessagingConfigurationContext discoveryContext, string name)
        : base(discoveryContext)
    {
        Configuration = new PostgresReceiveEndpointConfiguration { Name = name, QueueName = name };
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

    /// <inheritdoc />
    public new IPostgresReceiveEndpointDescriptor Handler<THandler>() where THandler : class, IHandler
    {
        base.Handler<THandler>();

        return this;
    }

    /// <inheritdoc />
    public new IPostgresReceiveEndpointDescriptor Handler(Type handlerType)
    {
        base.Handler(handlerType);

        return this;
    }

    /// <inheritdoc />
    public new IPostgresReceiveEndpointDescriptor Consumer(Type consumerType)
    {
        base.Consumer(consumerType);

        return this;
    }

    /// <inheritdoc />
    public new IPostgresReceiveEndpointDescriptor Consumer<TConsumer>() where TConsumer : class, IConsumer
    {
        base.Consumer<TConsumer>();

        return this;
    }

    /// <inheritdoc />
    public new IPostgresReceiveEndpointDescriptor Receives<TMessage>()
    {
        base.Receives<TMessage>();

        return this;
    }

    /// <inheritdoc />
    public new IPostgresReceiveEndpointDescriptor Receives<TMessage>(Action<IReceiveTypeBindDescriptor> configure)
    {
        base.Receives<TMessage>(configure);

        return this;
    }

    /// <inheritdoc />
    public new IPostgresReceiveEndpointDescriptor Receives(Type messageType)
    {
        base.Receives(messageType);

        return this;
    }

    /// <inheritdoc />
    public new IPostgresReceiveEndpointDescriptor AutoBind(bool enabled)
    {
        base.AutoBind(enabled);

        return this;
    }

    /// <inheritdoc />
    public new IPostgresReceiveEndpointDescriptor BindFrom(Uri source, string? routingKey = null)
    {
        base.BindFrom(source, routingKey);

        return this;
    }

    /// <inheritdoc />
    public new IPostgresReceiveEndpointDescriptor Kind(ReceiveEndpointKind kind)
    {
        base.Kind(kind);

        return this;
    }

    /// <inheritdoc />
    public new IPostgresReceiveEndpointDescriptor MaxConcurrency(int maxConcurrency)
    {
        base.MaxConcurrency(maxConcurrency);

        return this;
    }

    /// <inheritdoc />
    public new IPostgresReceiveEndpointDescriptor FaultEndpoint(string name)
    {
        base.FaultEndpoint(name);

        return this;
    }

    /// <inheritdoc />
    public new IPostgresReceiveEndpointDescriptor SkippedEndpoint(string name)
    {
        base.SkippedEndpoint(name);

        return this;
    }

    /// <inheritdoc />
    public IPostgresReceiveEndpointDescriptor Queue(string name)
    {
        if (_queueIdentityPinned)
        {
            throw ThrowHelper.QueueIdentityPinned(Configuration.QueueName ?? Configuration.Name ?? string.Empty);
        }

        Configuration.QueueName = name;

        return this;
    }

    /// <inheritdoc />
    public IPostgresReceiveEndpointDescriptor MaxBatchSize(int size)
    {
        Configuration.MaxBatchSize = size;

        return this;
    }

    /// <inheritdoc />
    public IPostgresReceiveEndpointDescriptor ErrorQueue(string name)
    {
        Configuration.ErrorQueue.QueueName = name;

        return this;
    }

    /// <inheritdoc />
    public IPostgresReceiveEndpointDescriptor DisableErrorQueue()
    {
        Configuration.ErrorQueue.IsDisabled = true;

        return this;
    }

    /// <inheritdoc />
    public IPostgresReceiveEndpointDescriptor SkippedQueue(string name)
    {
        Configuration.SkippedQueue.QueueName = name;

        return this;
    }

    /// <inheritdoc />
    public IPostgresReceiveEndpointDescriptor DisableSkippedQueue()
    {
        Configuration.SkippedQueue.IsDisabled = true;

        return this;
    }

    /// <inheritdoc />
    public new IPostgresReceiveEndpointDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null)
    {
        base.UseReceive(configuration, before: before, after: after);

        return this;
    }

    public PostgresReceiveEndpointConfiguration CreateConfiguration()
    {
        return Configuration;
    }

    public static PostgresReceiveEndpointDescriptor New(IMessagingConfigurationContext context, string name)
        => new(context, name);
}
