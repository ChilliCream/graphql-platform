namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Descriptor for configuring a RabbitMQ receive endpoint that consumes messages from a specific queue.
/// </summary>
internal sealed class RabbitMQReceiveEndpointDescriptor
    : ReceiveEndpointDescriptor<RabbitMQReceiveEndpointConfiguration>
    , IRabbitMQReceiveEndpointDescriptor
{
    private bool _queueIdentityPinned;

    private RabbitMQReceiveEndpointDescriptor(IMessagingConfigurationContext discoveryContext, string name)
        : base(discoveryContext)
    {
        Configuration = new RabbitMQReceiveEndpointConfiguration { Name = name, QueueName = name };
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
    public new IRabbitMQReceiveEndpointDescriptor Handler<THandler>() where THandler : class, IHandler
    {
        base.Handler<THandler>();

        return this;
    }

    public new IRabbitMQReceiveEndpointDescriptor Handler(Type handlerType)
    {
        base.Handler(handlerType);

        return this;
    }

    public new IRabbitMQReceiveEndpointDescriptor Consumer(Type consumerType)
    {
        base.Consumer(consumerType);

        return this;
    }

    public new IRabbitMQReceiveEndpointDescriptor Consumer<TConsumer>() where TConsumer : class, IConsumer
    {
        base.Consumer<TConsumer>();

        return this;
    }

    /// <inheritdoc />
    public new IRabbitMQReceiveEndpointDescriptor Receives<TMessage>()
    {
        base.Receives<TMessage>();

        return this;
    }

    /// <inheritdoc />
    public new IRabbitMQReceiveEndpointDescriptor Receives<TMessage>(Action<IReceiveTypeBindDescriptor> configure)
    {
        base.Receives<TMessage>(configure);

        return this;
    }

    /// <inheritdoc />
    public new IRabbitMQReceiveEndpointDescriptor Receives(Type messageType)
    {
        base.Receives(messageType);

        return this;
    }

    /// <inheritdoc />
    public new IRabbitMQReceiveEndpointDescriptor AutoBind(bool enabled)
    {
        base.AutoBind(enabled);

        return this;
    }

    /// <inheritdoc />
    public new IRabbitMQReceiveEndpointDescriptor BindFrom(Uri source, string? routingKey = null)
    {
        base.BindFrom(source, routingKey);

        return this;
    }

    /// <inheritdoc />
    public new IRabbitMQReceiveEndpointDescriptor Kind(ReceiveEndpointKind kind)
    {
        base.Kind(kind);

        return this;
    }

    /// <inheritdoc />
    public new IRabbitMQReceiveEndpointDescriptor MaxConcurrency(int maxConcurrency)
    {
        base.MaxConcurrency(maxConcurrency);

        return this;
    }

    /// <inheritdoc />
    public new IRabbitMQReceiveEndpointDescriptor FaultEndpoint(string name)
    {
        base.FaultEndpoint(name);

        return this;
    }

    /// <inheritdoc />
    public new IRabbitMQReceiveEndpointDescriptor SkippedEndpoint(string name)
    {
        base.SkippedEndpoint(name);

        return this;
    }

    /// <inheritdoc />
    public IRabbitMQReceiveEndpointDescriptor Queue(string name)
    {
        if (_queueIdentityPinned)
        {
            throw ThrowHelper.QueueIdentityPinned(Configuration.QueueName ?? Configuration.Name ?? string.Empty);
        }

        Configuration.QueueName = name;

        return this;
    }

    /// <inheritdoc />
    public IRabbitMQReceiveEndpointDescriptor MaxPrefetch(ushort maxPrefetch)
    {
        Configuration.MaxPrefetch = maxPrefetch;

        return this;
    }

    /// <inheritdoc />
    public IRabbitMQReceiveEndpointDescriptor ErrorQueue(string name)
    {
        Configuration.ErrorQueue.QueueName = name;

        return this;
    }

    /// <inheritdoc />
    public IRabbitMQReceiveEndpointDescriptor DisableErrorQueue()
    {
        Configuration.ErrorQueue.IsDisabled = true;

        return this;
    }

    /// <inheritdoc />
    public IRabbitMQReceiveEndpointDescriptor SkippedQueue(string name)
    {
        Configuration.SkippedQueue.QueueName = name;

        return this;
    }

    /// <inheritdoc />
    public IRabbitMQReceiveEndpointDescriptor DisableSkippedQueue()
    {
        Configuration.SkippedQueue.IsDisabled = true;

        return this;
    }

    /// <inheritdoc />
    public new IRabbitMQReceiveEndpointDescriptor UseReceive(
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
    /// <returns>The configured <see cref="RabbitMQReceiveEndpointConfiguration"/>.</returns>
    public RabbitMQReceiveEndpointConfiguration CreateConfiguration()
    {
        return Configuration;
    }

    /// <summary>
    /// Creates a new receive endpoint descriptor with the specified name, which also serves as the default queue name.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="name">The endpoint name and default queue name.</param>
    /// <returns>A new receive endpoint descriptor.</returns>
    public static RabbitMQReceiveEndpointDescriptor New(IMessagingConfigurationContext context, string name)
        => new(context, name);
}
