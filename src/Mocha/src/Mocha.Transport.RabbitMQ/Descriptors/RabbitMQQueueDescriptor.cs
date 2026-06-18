using Mocha.Features;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Composes a topology queue descriptor with a lazily created receive endpoint. Topology methods
/// configure the backing queue, while receive methods materialize an endpoint for the same queue.
/// </summary>
internal sealed class RabbitMQQueueDescriptor : IRabbitMQQueueDescriptor
{
    private readonly RabbitMQMessagingTransportDescriptor _transport;
    private readonly IRabbitMQQueueTopologyDescriptor _queue;
    private readonly string _name;
    private RabbitMQReceiveEndpointDescriptor? _endpoint;

    /// <summary>
    /// Creates a new descriptor for the given queue name, eagerly declaring the queue in the topology.
    /// </summary>
    /// <param name="transport">The owning transport descriptor.</param>
    /// <param name="name">The queue name, which also serves as the endpoint identity.</param>
    internal RabbitMQQueueDescriptor(RabbitMQMessagingTransportDescriptor transport, string name)
    {
        _transport = transport;
        _name = name;
        _queue = transport.DeclareQueue(name);
    }

    /// <summary>
    /// Gets the lazily created receive endpoint, or null if no routing method has been called.
    /// </summary>
    internal RabbitMQReceiveEndpointDescriptor? Endpoint => _endpoint;

    internal string Name => _name;

    private RabbitMQReceiveEndpointDescriptor EnsureEndpoint()
        => _endpoint ??= (RabbitMQReceiveEndpointDescriptor)_transport.Endpoint(_name);

    internal bool TryGetEntityOnlyEndpointToPrune(out RabbitMQReceiveEndpointDescriptor? endpoint)
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
    public IRabbitMQQueueDescriptor Durable(bool durable = true)
    {
        _queue.Durable(durable);
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor Quorum()
    {
        _queue.WithArgument("x-queue-type", RabbitMQQueueType.Quorum);
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor WithArgument(string key, object value)
    {
        _queue.WithArgument(key, value);
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor AutoProvision(bool autoProvision = true)
    {
        _queue.AutoProvision(autoProvision);
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor Handler<THandler>() where THandler : class, IHandler
    {
        EnsureEndpoint().Handler<THandler>();
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor Handler(Type handlerType)
    {
        EnsureEndpoint().Handler(handlerType);
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor Consumer<TConsumer>() where TConsumer : class, IConsumer
    {
        EnsureEndpoint().Consumer<TConsumer>();
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor Consumer(Type consumerType)
    {
        EnsureEndpoint().Consumer(consumerType);
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor Receives<TMessage>()
    {
        EnsureEndpoint().Receives<TMessage>();
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor Receives(Type messageType)
    {
        EnsureEndpoint().Receives(messageType);
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor BindImplicitly()
    {
        EnsureEndpoint().BindImplicitly();
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor BindExplicitly()
    {
        EnsureEndpoint().BindExplicitly();
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor MaxPrefetch(ushort maxPrefetch)
    {
        EnsureEndpoint().MaxPrefetch(maxPrefetch);
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor Kind(ReceiveEndpointKind kind)
    {
        EnsureEndpoint().Kind(kind);
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor MaxConcurrency(int maxConcurrency)
    {
        EnsureEndpoint().MaxConcurrency(maxConcurrency);
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null)
    {
        EnsureEndpoint().UseReceive(configuration, before, after);
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor FaultEndpoint(string name)
    {
        EnsureEndpoint().FaultEndpoint(name);
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor SkippedEndpoint(string name)
    {
        EnsureEndpoint().SkippedEndpoint(name);
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor ErrorQueue(string name)
    {
        var feature = EnsureEndpoint().Configuration.Features.GetOrSet<ReceiveFaultEndpointFeature>();
        feature.IsDisabled = false;
        feature.QueueName = name;
        feature.Address = null;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor DisableErrorQueue()
    {
        var feature = EnsureEndpoint().Configuration.Features.GetOrSet<ReceiveFaultEndpointFeature>();
        feature.IsDisabled = true;
        feature.QueueName = null;
        feature.Address = null;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor SkippedQueue(string name)
    {
        var feature = EnsureEndpoint().Configuration.Features.GetOrSet<ReceiveSkippedEndpointFeature>();
        feature.IsDisabled = false;
        feature.QueueName = name;
        feature.Address = null;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor DisableSkippedQueue()
    {
        var feature = EnsureEndpoint().Configuration.Features.GetOrSet<ReceiveSkippedEndpointFeature>();
        feature.IsDisabled = true;
        feature.QueueName = null;
        feature.Address = null;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueDescriptor BindFrom(Uri source, string? routingKey = null)
    {
        ArgumentNullException.ThrowIfNull(source);

        var schema = _transport.Configuration.Schema ?? RabbitMQTransportConfiguration.DefaultSchema;

        if (!RabbitMQDestinations.TryResolveSourceExchange(schema, source, out var exchangeName))
        {
            throw new InvalidOperationException(
                $"BindFrom source '{source}' could not be resolved to a RabbitMQ exchange name.");
        }

        _transport.DeclareExchange(exchangeName);
        var binding = _transport.DeclareBinding(exchangeName, _name);
        if (routingKey is not null)
        {
            binding.RoutingKey(routingKey);
        }

        return this;
    }
}
