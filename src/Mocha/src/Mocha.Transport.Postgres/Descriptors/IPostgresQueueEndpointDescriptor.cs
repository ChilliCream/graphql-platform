namespace Mocha.Transport.Postgres;

/// <summary>
/// Fluent interface for configuring a PostgreSQL endpoint whose identity is fixed to a named queue.
/// This is the unified front-door handle returned by <c>t.Queue(name, q => ...)</c>.
/// All consumer, binding, and auto-bind members are available here; the queue name cannot be changed
/// after creation.
/// </summary>
public interface IPostgresQueueEndpointDescriptor : IPostgresReceiveEndpointDescriptor
{
    /// <inheritdoc cref="IPostgresReceiveEndpointDescriptor.Handler{THandler}" />
    new IPostgresQueueEndpointDescriptor Handler<THandler>() where THandler : class, IHandler;

    /// <inheritdoc cref="IPostgresReceiveEndpointDescriptor.Handler(Type)" />
    new IPostgresQueueEndpointDescriptor Handler(Type handlerType);

    /// <inheritdoc cref="IPostgresReceiveEndpointDescriptor.Consumer(Type)" />
    new IPostgresQueueEndpointDescriptor Consumer(Type consumerType);

    /// <inheritdoc cref="IPostgresReceiveEndpointDescriptor.Consumer{TConsumer}" />
    new IPostgresQueueEndpointDescriptor Consumer<TConsumer>() where TConsumer : class, IConsumer;

    /// <inheritdoc cref="IPostgresReceiveEndpointDescriptor.Receives{TMessage}()" />
    new IPostgresQueueEndpointDescriptor Receives<TMessage>();

    /// <inheritdoc cref="IPostgresReceiveEndpointDescriptor.Receives{TMessage}(Action{IReceiveTypeBindDescriptor})" />
    new IPostgresQueueEndpointDescriptor Receives<TMessage>(Action<IReceiveTypeBindDescriptor> configure);

    /// <inheritdoc cref="IPostgresReceiveEndpointDescriptor.Receives(Type)" />
    new IPostgresQueueEndpointDescriptor Receives(Type messageType);

    /// <inheritdoc cref="IPostgresReceiveEndpointDescriptor.AutoBind(bool)" />
    new IPostgresQueueEndpointDescriptor AutoBind(bool enabled);

    /// <inheritdoc cref="IPostgresReceiveEndpointDescriptor.BindFrom(Uri, string?)" />
    new IPostgresQueueEndpointDescriptor BindFrom(Uri source, string? routingKey = null);

    /// <inheritdoc cref="IPostgresReceiveEndpointDescriptor.Kind(ReceiveEndpointKind)" />
    new IPostgresQueueEndpointDescriptor Kind(ReceiveEndpointKind kind);

    /// <inheritdoc cref="IPostgresReceiveEndpointDescriptor.ErrorQueue(string)" />
    new IPostgresQueueEndpointDescriptor ErrorQueue(string name);

    /// <inheritdoc cref="IPostgresReceiveEndpointDescriptor.DisableErrorQueue()" />
    new IPostgresQueueEndpointDescriptor DisableErrorQueue();

    /// <inheritdoc cref="IPostgresReceiveEndpointDescriptor.SkippedQueue(string)" />
    new IPostgresQueueEndpointDescriptor SkippedQueue(string name);

    /// <inheritdoc cref="IPostgresReceiveEndpointDescriptor.DisableSkippedQueue()" />
    new IPostgresQueueEndpointDescriptor DisableSkippedQueue();

    /// <inheritdoc cref="IPostgresReceiveEndpointDescriptor.MaxConcurrency(int)" />
    new IPostgresQueueEndpointDescriptor MaxConcurrency(int maxConcurrency);

    /// <inheritdoc cref="IPostgresReceiveEndpointDescriptor.FaultEndpoint(string)" />
    new IPostgresQueueEndpointDescriptor FaultEndpoint(string name);

    /// <inheritdoc cref="IPostgresReceiveEndpointDescriptor.SkippedEndpoint(string)" />
    new IPostgresQueueEndpointDescriptor SkippedEndpoint(string name);

    /// <inheritdoc cref="IPostgresReceiveEndpointDescriptor.MaxBatchSize(int)" />
    new IPostgresQueueEndpointDescriptor MaxBatchSize(int size);

    /// <inheritdoc cref="IPostgresReceiveEndpointDescriptor.UseReceive" />
    new IPostgresQueueEndpointDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null);

    /// <summary>
    /// Sets whether the backing queue is automatically provisioned in the database.
    /// </summary>
    /// <param name="autoProvision">True to provision the queue; otherwise false.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IPostgresQueueEndpointDescriptor AutoProvision(bool autoProvision = true);

    /// <summary>
    /// Not supported on a unified queue endpoint. The queue name is fixed at creation.
    /// Use <c>t.Queue(name, q => ...)</c> to set the queue name.
    /// </summary>
    [Obsolete(
        "Queue identity is fixed on a unified queue endpoint. "
        + "The queue name cannot be changed after creation. "
        + "Use t.Queue(name, q => ...) to configure the endpoint with a specific queue name.",
        error: true)]
    new IPostgresQueueEndpointDescriptor Queue(string name);
}
