namespace Mocha.Transport.InMemory;

/// <summary>
/// Composes a topology queue descriptor with a lazily created receive endpoint. When only infra
/// methods are called (BindFrom), no endpoint is materialized. Routing methods (Consumer, Handler,
/// Receives, etc.) trigger lazy endpoint creation via the transport's idempotent <c>Endpoint(name)</c>.
/// </summary>
internal sealed class InMemoryQueueBuilder : IInMemoryQueueBuilder
{
    private readonly InMemoryMessagingTransportDescriptor _transport;
    private readonly IInMemoryQueueDescriptor _queue;
    private readonly string _name;
    private InMemoryReceiveEndpointDescriptor? _endpoint;

    /// <summary>
    /// Creates a new builder for the given queue name, eagerly declaring the queue in the topology.
    /// </summary>
    /// <param name="transport">The owning transport descriptor.</param>
    /// <param name="name">The queue name, which also serves as the endpoint identity.</param>
    internal InMemoryQueueBuilder(InMemoryMessagingTransportDescriptor transport, string name)
    {
        _transport = transport;
        _name = name;
        _queue = transport.DeclareQueue(name);
    }

    /// <summary>
    /// Gets the lazily created receive endpoint, or null if no routing method has been called.
    /// </summary>
    internal InMemoryReceiveEndpointDescriptor? Endpoint => _endpoint;

    private InMemoryReceiveEndpointDescriptor EnsureEndpoint()
        => _endpoint ??= (InMemoryReceiveEndpointDescriptor)_transport.Endpoint(_name);

    // -- Routing group --

    /// <inheritdoc />
    public IInMemoryQueueBuilder Handler<THandler>() where THandler : class, IHandler
    {
        EnsureEndpoint().Handler<THandler>();
        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueBuilder Handler(Type handlerType)
    {
        EnsureEndpoint().Handler(handlerType);
        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueBuilder Consumer<TConsumer>() where TConsumer : class, IConsumer
    {
        EnsureEndpoint().Consumer<TConsumer>();
        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueBuilder Consumer(Type consumerType)
    {
        EnsureEndpoint().Consumer(consumerType);
        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueBuilder Receives<TMessage>()
    {
        EnsureEndpoint().Receives<TMessage>();
        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueBuilder Receives(Type messageType)
    {
        EnsureEndpoint().Receives(messageType);
        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueBuilder BindImplicitly()
    {
        EnsureEndpoint().BindImplicitly();
        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueBuilder BindExplicitly()
    {
        EnsureEndpoint().BindExplicitly();
        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueBuilder MaxConcurrency(int maxConcurrency)
    {
        EnsureEndpoint().MaxConcurrency(maxConcurrency);
        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueBuilder Kind(ReceiveEndpointKind kind)
    {
        EnsureEndpoint().Kind(kind);
        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueBuilder UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null)
    {
        EnsureEndpoint().UseReceive(configuration, before, after);
        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueBuilder FaultEndpoint(string name)
    {
        EnsureEndpoint().FaultEndpoint(name);
        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueBuilder SkippedEndpoint(string name)
    {
        EnsureEndpoint().SkippedEndpoint(name);
        return this;
    }

    // -- BindFrom (infra group, writes directly to topology) --

    /// <inheritdoc />
    public IInMemoryQueueBuilder BindFrom(Uri source, string? routingKey = null)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (routingKey is not null)
        {
            throw new InvalidOperationException(
                "The in-memory transport does not support routing key semantics. "
                + $"Queue '{_name}' cannot use a routing key in BindFrom.");
        }

        var resolver = new InMemoryDestinationResolver(
            _transport.Configuration.Schema ?? InMemoryTransportConfiguration.DefaultSchema);

        if (!resolver.TryResolveSourceTopic(source, out var topicName))
        {
            throw new InvalidOperationException(
                $"BindFrom source '{source}' could not be resolved to an in-memory topic name.");
        }

        _transport.DeclareTopic(topicName);
        _transport.DeclareBinding(topicName, _name);

        return this;
    }

    // -- Escape hatches --

    /// <inheritdoc />
    public IInMemoryQueueDescriptor AsQueue() => _queue;

    /// <inheritdoc />
    public IInMemoryReceiveEndpointDescriptor AsEndpoint() => EnsureEndpoint();
}
