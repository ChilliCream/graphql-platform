namespace Mocha.Transport.Postgres;

/// <summary>
/// Fluent interface for configuring a PostgreSQL receive endpoint, including its backing queue,
/// handlers, batch size, and receive middleware pipeline.
/// </summary>
public interface IPostgresReceiveEndpointDescriptor : IReceiveEndpointDescriptor<PostgresReceiveEndpointConfiguration>
{
    /// <inheritdoc cref="IReceiveEndpointDescriptor{T}.Handler{THandler}"/>
    new IPostgresReceiveEndpointDescriptor Handler<THandler>() where THandler : class, IHandler;

    /// <inheritdoc cref="IReceiveEndpointDescriptor{T}.Handler(Type)"/>
    new IPostgresReceiveEndpointDescriptor Handler(Type handlerType);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{T}.Consumer(Type)"/>
    new IPostgresReceiveEndpointDescriptor Consumer(Type consumerType);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{T}.Consumer{TConsumer}"/>
    new IPostgresReceiveEndpointDescriptor Consumer<TConsumer>() where TConsumer : class, IConsumer;

    /// <inheritdoc />
    new IPostgresReceiveEndpointDescriptor Receives<TMessage>();

    /// <inheritdoc cref="IReceiveEndpointDescriptor{T}.Receives{TMessage}(Action{IReceiveTypeBindDescriptor})"/>
    new IPostgresReceiveEndpointDescriptor Receives<TMessage>(Action<IReceiveTypeBindDescriptor> configure);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{T}.Receives(Type)"/>
    new IPostgresReceiveEndpointDescriptor Receives(Type messageType);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{T}.AutoBind"/>
    new IPostgresReceiveEndpointDescriptor AutoBind(bool enabled);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{T}.BindFrom"/>
    new IPostgresReceiveEndpointDescriptor BindFrom(Uri source, string? routingKey = null);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{T}.Kind(ReceiveEndpointKind)"/>
    new IPostgresReceiveEndpointDescriptor Kind(ReceiveEndpointKind kind);

    /// <summary>
    /// Sets the verbatim name of the error queue satellite for this endpoint.
    /// The name is stored exactly as provided; no convention-based transformation is applied.
    /// </summary>
    /// <param name="name">The exact queue name to use for the error satellite.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IPostgresReceiveEndpointDescriptor ErrorQueue(string name);

    /// <summary>
    /// Disables the error queue satellite for this endpoint.
    /// When disabled, failed messages are not forwarded to an error queue.
    /// </summary>
    /// <returns>The descriptor for method chaining.</returns>
    IPostgresReceiveEndpointDescriptor DisableErrorQueue();

    /// <summary>
    /// Sets the verbatim name of the skipped queue satellite for this endpoint.
    /// The name is stored exactly as provided; no convention-based transformation is applied.
    /// </summary>
    /// <param name="name">The exact queue name to use for the skipped satellite.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IPostgresReceiveEndpointDescriptor SkippedQueue(string name);

    /// <summary>
    /// Disables the skipped queue satellite for this endpoint.
    /// When disabled, unrecognized messages are not forwarded to a skipped queue.
    /// </summary>
    /// <returns>The descriptor for method chaining.</returns>
    IPostgresReceiveEndpointDescriptor DisableSkippedQueue();

    /// <inheritdoc cref="IReceiveEndpointDescriptor{T}.MaxConcurrency(int)"/>
    new IPostgresReceiveEndpointDescriptor MaxConcurrency(int maxConcurrency);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{T}.FaultEndpoint"/>
    new IPostgresReceiveEndpointDescriptor FaultEndpoint(string name);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{T}.SkippedEndpoint"/>
    new IPostgresReceiveEndpointDescriptor SkippedEndpoint(string name);

    /// <summary>
    /// Sets the name of the PostgreSQL queue this endpoint will consume from.
    /// </summary>
    /// <param name="name">The queue name.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IPostgresReceiveEndpointDescriptor Queue(string name);

    /// <summary>
    /// Sets the maximum number of messages to fetch per batch.
    /// </summary>
    /// <param name="size">The batch size.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IPostgresReceiveEndpointDescriptor MaxBatchSize(int size);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{T}.UseReceive(ReceiveMiddlewareConfiguration, string?, string?)"/>
    new IPostgresReceiveEndpointDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null);
}
