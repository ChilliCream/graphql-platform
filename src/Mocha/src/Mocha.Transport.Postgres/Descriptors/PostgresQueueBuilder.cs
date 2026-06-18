namespace Mocha.Transport.Postgres;

/// <summary>
/// Composes a topology queue descriptor with a lazily created receive endpoint. When only infra
/// methods are called (AutoProvision, AutoDelete), no endpoint is materialized. Routing methods
/// (Consumer, Handler, Receives, etc.) trigger lazy endpoint creation via the transport's
/// idempotent <c>Endpoint(name)</c>.
/// </summary>
internal sealed class PostgresQueueBuilder : IPostgresQueueBuilder
{
    private readonly PostgresMessagingTransportDescriptor _transport;
    private readonly IPostgresQueueDescriptor _queue;
    private readonly string _name;
    private PostgresReceiveEndpointDescriptor? _endpoint;
    private string? _errorQueueName;
    private string? _skippedQueueName;

    /// <summary>
    /// Creates a new builder for the given queue name, eagerly declaring the queue in the topology.
    /// </summary>
    /// <param name="transport">The owning transport descriptor.</param>
    /// <param name="name">The queue name, which also serves as the endpoint identity.</param>
    internal PostgresQueueBuilder(PostgresMessagingTransportDescriptor transport, string name)
    {
        _transport = transport;
        _name = name;
        _queue = transport.DeclareQueue(name);
    }

    /// <summary>
    /// Gets the lazily created receive endpoint, or null if no routing method has been called.
    /// </summary>
    internal PostgresReceiveEndpointDescriptor? Endpoint => _endpoint;

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

    private PostgresReceiveEndpointDescriptor EnsureEndpoint()
        => _endpoint ??= (PostgresReceiveEndpointDescriptor)_transport.Endpoint(_name);

    /// <inheritdoc />
    public IPostgresQueueBuilder AutoProvision(bool autoProvision = true)
    {
        _queue.AutoProvision(autoProvision);
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueBuilder AutoDelete(bool autoDelete = true)
    {
        _queue.AutoDelete(autoDelete);
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueBuilder Handler<THandler>() where THandler : class, IHandler
    {
        EnsureEndpoint().Handler<THandler>();
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueBuilder Handler(Type handlerType)
    {
        EnsureEndpoint().Handler(handlerType);
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueBuilder Consumer<TConsumer>() where TConsumer : class, IConsumer
    {
        EnsureEndpoint().Consumer<TConsumer>();
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueBuilder Consumer(Type consumerType)
    {
        EnsureEndpoint().Consumer(consumerType);
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueBuilder Receives<TMessage>()
    {
        EnsureEndpoint().Receives<TMessage>();
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueBuilder Receives(Type messageType)
    {
        EnsureEndpoint().Receives(messageType);
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueBuilder BindImplicitly()
    {
        EnsureEndpoint().BindImplicitly();
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueBuilder BindExplicitly()
    {
        EnsureEndpoint().BindExplicitly();
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueBuilder MaxBatchSize(int size)
    {
        EnsureEndpoint().MaxBatchSize(size);
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueBuilder Kind(ReceiveEndpointKind kind)
    {
        EnsureEndpoint().Kind(kind);
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueBuilder MaxConcurrency(int maxConcurrency)
    {
        EnsureEndpoint().MaxConcurrency(maxConcurrency);
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueBuilder UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null)
    {
        EnsureEndpoint().UseReceive(configuration, before, after);
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueBuilder FaultEndpoint(string name)
    {
        _errorQueueName = null;
        EnsureEndpoint().FaultEndpoint(name);
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueBuilder SkippedEndpoint(string name)
    {
        _skippedQueueName = null;
        EnsureEndpoint().SkippedEndpoint(name);
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueBuilder ErrorQueue(string name)
    {
        _errorQueueName = name;
        var configuration = EnsureEndpoint().Configuration;
        configuration.IsErrorEndpointDisabled = false;
        configuration.ErrorEndpoint = null;
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueBuilder DisableErrorQueue()
    {
        _errorQueueName = null;
        var configuration = EnsureEndpoint().Configuration;
        configuration.IsErrorEndpointDisabled = true;
        configuration.ErrorEndpoint = null;
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueBuilder SkippedQueue(string name)
    {
        _skippedQueueName = name;
        var configuration = EnsureEndpoint().Configuration;
        configuration.IsSkippedEndpointDisabled = false;
        configuration.SkippedEndpoint = null;
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueBuilder DisableSkippedQueue()
    {
        _skippedQueueName = null;
        var configuration = EnsureEndpoint().Configuration;
        configuration.IsSkippedEndpointDisabled = true;
        configuration.SkippedEndpoint = null;
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueBuilder BindFrom(Uri source, string? routingKey = null)
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

    private static Uri CreateQueueUri(string schema, string name)
        => new($"{schema}:q/{name}");
}
