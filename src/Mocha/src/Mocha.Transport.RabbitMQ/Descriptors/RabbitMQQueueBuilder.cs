namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Composes a topology queue descriptor with a lazily created receive endpoint. When only infra
/// methods are called (Durable, WithArgument, etc.), no endpoint is materialized. Routing methods
/// (Consumer, Handler, Receives, etc.) trigger lazy endpoint creation via the transport's
/// idempotent <c>Endpoint(name)</c>.
/// </summary>
internal sealed class RabbitMQQueueBuilder : IRabbitMQQueueBuilder
{
    private readonly RabbitMQMessagingTransportDescriptor _transport;
    private readonly IRabbitMQQueueDescriptor _queue;
    private readonly string _name;
    private RabbitMQReceiveEndpointDescriptor? _endpoint;
    private string? _errorQueueName;
    private string? _skippedQueueName;

    /// <summary>
    /// Creates a new builder for the given queue name, eagerly declaring the queue in the topology.
    /// </summary>
    /// <param name="transport">The owning transport descriptor.</param>
    /// <param name="name">The queue name, which also serves as the endpoint identity.</param>
    internal RabbitMQQueueBuilder(RabbitMQMessagingTransportDescriptor transport, string name)
    {
        _transport = transport;
        _name = name;
        _queue = transport.DeclareQueue(name);
    }

    /// <summary>
    /// Gets the lazily created receive endpoint, or null if no routing method has been called.
    /// </summary>
    internal RabbitMQReceiveEndpointDescriptor? Endpoint => _endpoint;

    internal void MaterializeFaultAndSkippedQueueRoutes(string schema)
    {
        if (_endpoint is null)
        {
            return;
        }

        var configuration = _endpoint.Configuration;
        if (_errorQueueName is not null && !configuration.IsErrorEndpointDisabled)
        {
            configuration.ErrorEndpoint = CreateQueueUri(schema, _errorQueueName);
        }

        if (_skippedQueueName is not null && !configuration.IsSkippedEndpointDisabled)
        {
            configuration.SkippedEndpoint = CreateQueueUri(schema, _skippedQueueName);
        }
    }

    private RabbitMQReceiveEndpointDescriptor EnsureEndpoint()
        => _endpoint ??= (RabbitMQReceiveEndpointDescriptor)_transport.Endpoint(_name);

    /// <inheritdoc />
    public IRabbitMQQueueBuilder Durable(bool durable = true)
    {
        _queue.Durable(durable);
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueBuilder Quorum()
    {
        _queue.WithArgument("x-queue-type", RabbitMQQueueType.Quorum);
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueBuilder WithArgument(string key, object value)
    {
        _queue.WithArgument(key, value);
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueBuilder AutoProvision(bool autoProvision = true)
    {
        _queue.AutoProvision(autoProvision);
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueBuilder Handler<THandler>() where THandler : class, IHandler
    {
        EnsureEndpoint().Handler<THandler>();
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueBuilder Handler(Type handlerType)
    {
        EnsureEndpoint().Handler(handlerType);
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueBuilder Consumer<TConsumer>() where TConsumer : class, IConsumer
    {
        EnsureEndpoint().Consumer<TConsumer>();
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueBuilder Consumer(Type consumerType)
    {
        EnsureEndpoint().Consumer(consumerType);
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueBuilder Receives<TMessage>()
    {
        EnsureEndpoint().Receives<TMessage>();
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueBuilder Receives(Type messageType)
    {
        EnsureEndpoint().Receives(messageType);
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueBuilder BindImplicitly()
    {
        EnsureEndpoint().BindImplicitly();
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueBuilder BindExplicitly()
    {
        EnsureEndpoint().BindExplicitly();
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueBuilder MaxPrefetch(ushort maxPrefetch)
    {
        EnsureEndpoint().MaxPrefetch(maxPrefetch);
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueBuilder Kind(ReceiveEndpointKind kind)
    {
        EnsureEndpoint().Kind(kind);
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueBuilder MaxConcurrency(int maxConcurrency)
    {
        EnsureEndpoint().MaxConcurrency(maxConcurrency);
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueBuilder UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null)
    {
        EnsureEndpoint().UseReceive(configuration, before, after);
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueBuilder FaultEndpoint(string name)
    {
        _errorQueueName = null;
        EnsureEndpoint().FaultEndpoint(name);
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueBuilder SkippedEndpoint(string name)
    {
        _skippedQueueName = null;
        EnsureEndpoint().SkippedEndpoint(name);
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueBuilder ErrorQueue(string name)
    {
        _errorQueueName = name;
        var configuration = EnsureEndpoint().Configuration;
        configuration.IsErrorEndpointDisabled = false;
        configuration.ErrorEndpoint = null;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueBuilder DisableErrorQueue()
    {
        _errorQueueName = null;
        var configuration = EnsureEndpoint().Configuration;
        configuration.IsErrorEndpointDisabled = true;
        configuration.ErrorEndpoint = null;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueBuilder SkippedQueue(string name)
    {
        _skippedQueueName = name;
        var configuration = EnsureEndpoint().Configuration;
        configuration.IsSkippedEndpointDisabled = false;
        configuration.SkippedEndpoint = null;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueBuilder DisableSkippedQueue()
    {
        _skippedQueueName = null;
        var configuration = EnsureEndpoint().Configuration;
        configuration.IsSkippedEndpointDisabled = true;
        configuration.SkippedEndpoint = null;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueBuilder BindFrom(Uri source, string? routingKey = null)
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

    private static Uri CreateQueueUri(string schema, string name)
        => new($"{schema}:q/{name}");
}
