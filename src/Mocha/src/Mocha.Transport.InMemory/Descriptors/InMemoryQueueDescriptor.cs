using Mocha.Features;

namespace Mocha.Transport.InMemory;

/// <summary>
/// Composes a topology queue descriptor with a lazily created receive endpoint. Topology methods
/// configure the backing queue, while receive methods materialize an endpoint for the same queue.
/// </summary>
internal sealed class InMemoryQueueDescriptor : IInMemoryQueueDescriptor
{
    private readonly InMemoryMessagingTransportDescriptor _transport;
    private readonly string _name;
    private InMemoryReceiveEndpointDescriptor? _endpoint;

    /// <summary>
    /// Creates a new descriptor for the given queue name, eagerly declaring the queue in the topology.
    /// </summary>
    /// <param name="transport">The owning transport descriptor.</param>
    /// <param name="name">The queue name, which also serves as the endpoint identity.</param>
    internal InMemoryQueueDescriptor(InMemoryMessagingTransportDescriptor transport, string name)
    {
        _transport = transport;
        _name = name;
        transport.DeclareQueue(name);
    }

    /// <summary>
    /// Gets the lazily created receive endpoint, or null if no routing method has been called.
    /// </summary>
    internal InMemoryReceiveEndpointDescriptor? Endpoint => _endpoint;

    internal string Name => _name;

    private InMemoryReceiveEndpointDescriptor EnsureEndpoint()
        => _endpoint ??= (InMemoryReceiveEndpointDescriptor)_transport.Endpoint(_name);

    internal bool TryGetEntityOnlyEndpointToPrune(out InMemoryReceiveEndpointDescriptor? endpoint)
    {
        endpoint = _endpoint;
        if (endpoint is null)
        {
            return false;
        }

        var configuration = endpoint.Configuration;
        if (configuration.ConsumerIdentities.Count != 0 || configuration.ReceivedMessageTypes.Count != 0)
        {
            return false;
        }

        var queueName = configuration.QueueName ?? configuration.Name ?? string.Empty;

        if (configuration.Features.Get<ReceiveFaultEndpointFeature>()
            is { Address: not null } or { QueueName: not null } or { IsDisabled: true })
        {
            throw ThrowHelper.FaultOrSkippedQueueRequiresConsumingEndpoint("error", queueName);
        }

        if (configuration.Features.Get<ReceiveSkippedEndpointFeature>()
            is { Address: not null } or { QueueName: not null } or { IsDisabled: true })
        {
            throw ThrowHelper.FaultOrSkippedQueueRequiresConsumingEndpoint("skipped", queueName);
        }

        return true;
    }

    /// <inheritdoc />
    public IInMemoryQueueDescriptor Handler<THandler>() where THandler : class, IHandler
    {
        EnsureEndpoint().Handler<THandler>();
        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueDescriptor Handler(Type handlerType)
    {
        EnsureEndpoint().Handler(handlerType);
        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueDescriptor Consumer<TConsumer>() where TConsumer : class, IConsumer
    {
        EnsureEndpoint().Consumer<TConsumer>();
        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueDescriptor Consumer(Type consumerType)
    {
        EnsureEndpoint().Consumer(consumerType);
        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueDescriptor Receives<TMessage>()
    {
        EnsureEndpoint().Receives<TMessage>();
        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueDescriptor Receives(Type messageType)
    {
        EnsureEndpoint().Receives(messageType);
        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueDescriptor BindImplicitly()
    {
        EnsureEndpoint().BindImplicitly();
        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueDescriptor BindExplicitly()
    {
        EnsureEndpoint().BindExplicitly();
        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueDescriptor MaxConcurrency(int maxConcurrency)
    {
        EnsureEndpoint().MaxConcurrency(maxConcurrency);
        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueDescriptor Kind(ReceiveEndpointKind kind)
    {
        EnsureEndpoint().Kind(kind);
        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null)
    {
        EnsureEndpoint().UseReceive(configuration, before, after);
        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueDescriptor FaultEndpoint(string name)
    {
        EnsureEndpoint().FaultEndpoint(name);
        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueDescriptor SkippedEndpoint(string name)
    {
        EnsureEndpoint().SkippedEndpoint(name);
        return this;
    }

    /// <inheritdoc />
    public IInMemoryQueueDescriptor BindFrom(Uri source, string? routingKey = null)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (routingKey is not null)
        {
            throw new InvalidOperationException(
                "The in-memory transport does not support routing key semantics. "
                + $"Queue '{_name}' cannot use a routing key in BindFrom.");
        }

        var schema = _transport.Configuration.Schema ?? InMemoryTransportConfiguration.DefaultSchema;

        if (!InMemoryDestinations.TryResolveSourceTopic(schema, source, out var topicName))
        {
            throw new InvalidOperationException(
                $"BindFrom source '{source}' could not be resolved to an in-memory topic name.");
        }

        _transport.DeclareTopic(topicName);
        _transport.DeclareBinding(topicName, _name);

        return this;
    }
}
