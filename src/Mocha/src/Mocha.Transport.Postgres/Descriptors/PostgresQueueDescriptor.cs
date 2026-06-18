using Mocha.Features;

namespace Mocha.Transport.Postgres;

/// <summary>
/// Composes a topology queue descriptor with a lazily created receive endpoint. Topology methods
/// configure the backing queue, while receive methods materialize an endpoint for the same queue.
/// </summary>
internal sealed class PostgresQueueDescriptor : IPostgresQueueDescriptor
{
    private readonly PostgresMessagingTransportDescriptor _transport;
    private readonly IPostgresQueueTopologyDescriptor _queue;
    private readonly string _name;
    private PostgresReceiveEndpointDescriptor? _endpoint;

    /// <summary>
    /// Creates a new descriptor for the given queue name, eagerly declaring the queue in the topology.
    /// </summary>
    /// <param name="transport">The owning transport descriptor.</param>
    /// <param name="name">The queue name, which also serves as the endpoint identity.</param>
    internal PostgresQueueDescriptor(PostgresMessagingTransportDescriptor transport, string name)
    {
        _transport = transport;
        _name = name;
        _queue = transport.DeclareQueue(name);
    }

    /// <summary>
    /// Gets the lazily created receive endpoint, or null if no routing method has been called.
    /// </summary>
    internal PostgresReceiveEndpointDescriptor? Endpoint => _endpoint;

    internal string Name => _name;

    private PostgresReceiveEndpointDescriptor EnsureEndpoint()
        => _endpoint ??= (PostgresReceiveEndpointDescriptor)_transport.Endpoint(_name);

    internal bool TryGetEntityOnlyEndpointToPrune(out PostgresReceiveEndpointDescriptor? endpoint)
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
    public IPostgresQueueDescriptor AutoProvision(bool autoProvision = true)
    {
        _queue.AutoProvision(autoProvision);
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueDescriptor AutoDelete(bool autoDelete = true)
    {
        _queue.AutoDelete(autoDelete);
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueDescriptor Handler<THandler>() where THandler : class, IHandler
    {
        EnsureEndpoint().Handler<THandler>();
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueDescriptor Handler(Type handlerType)
    {
        EnsureEndpoint().Handler(handlerType);
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueDescriptor Consumer<TConsumer>() where TConsumer : class, IConsumer
    {
        EnsureEndpoint().Consumer<TConsumer>();
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueDescriptor Consumer(Type consumerType)
    {
        EnsureEndpoint().Consumer(consumerType);
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueDescriptor Receives<TMessage>()
    {
        EnsureEndpoint().Receives<TMessage>();
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueDescriptor Receives(Type messageType)
    {
        EnsureEndpoint().Receives(messageType);
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueDescriptor BindImplicitly()
    {
        EnsureEndpoint().BindImplicitly();
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueDescriptor BindExplicitly()
    {
        EnsureEndpoint().BindExplicitly();
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueDescriptor MaxBatchSize(int size)
    {
        EnsureEndpoint().MaxBatchSize(size);
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueDescriptor Kind(ReceiveEndpointKind kind)
    {
        EnsureEndpoint().Kind(kind);
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueDescriptor MaxConcurrency(int maxConcurrency)
    {
        EnsureEndpoint().MaxConcurrency(maxConcurrency);
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null)
    {
        EnsureEndpoint().UseReceive(configuration, before, after);
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueDescriptor FaultEndpoint(string name)
    {
        EnsureEndpoint().FaultEndpoint(name);
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueDescriptor SkippedEndpoint(string name)
    {
        EnsureEndpoint().SkippedEndpoint(name);
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueDescriptor ErrorQueue(string name)
    {
        var feature = EnsureEndpoint().Configuration.Features.GetOrSet<ReceiveFaultEndpointFeature>();
        feature.IsDisabled = false;
        feature.QueueName = name;
        feature.Address = null;
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueDescriptor DisableErrorQueue()
    {
        var feature = EnsureEndpoint().Configuration.Features.GetOrSet<ReceiveFaultEndpointFeature>();
        feature.IsDisabled = true;
        feature.QueueName = null;
        feature.Address = null;
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueDescriptor SkippedQueue(string name)
    {
        var feature = EnsureEndpoint().Configuration.Features.GetOrSet<ReceiveSkippedEndpointFeature>();
        feature.IsDisabled = false;
        feature.QueueName = name;
        feature.Address = null;
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueDescriptor DisableSkippedQueue()
    {
        var feature = EnsureEndpoint().Configuration.Features.GetOrSet<ReceiveSkippedEndpointFeature>();
        feature.IsDisabled = true;
        feature.QueueName = null;
        feature.Address = null;
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueDescriptor BindFrom(Uri source, string? routingKey = null)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (routingKey is not null)
        {
            throw ThrowHelper.BindFromWithNonNullRoutingKey(
                "PostgreSQL",
                source.ToString(),
                _name);
        }

        var schema = _transport.Configuration.Schema ?? PostgresTransportConfiguration.DefaultSchema;

        if (!PostgresDestinations.TryResolveSourceTopic(schema, source, out var topicName))
        {
            throw new InvalidOperationException(
                $"BindFrom source '{source}' could not be resolved to a PostgreSQL topic name.");
        }

        _transport.DeclareTopic(topicName);
        _transport.DeclareSubscription(topicName, _name);

        return this;
    }
}
