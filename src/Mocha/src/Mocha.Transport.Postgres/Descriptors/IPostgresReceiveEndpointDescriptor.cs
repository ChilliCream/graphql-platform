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

    /// <inheritdoc cref="IReceiveEndpointDescriptor{T}.Kind(ReceiveEndpointKind)"/>
    new IPostgresReceiveEndpointDescriptor Kind(ReceiveEndpointKind kind);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{T}.FaultEndpoint(string)"/>
    new IPostgresReceiveEndpointDescriptor FaultEndpoint(string name);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{T}.SkippedEndpoint(string)"/>
    new IPostgresReceiveEndpointDescriptor SkippedEndpoint(string name);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{T}.MaxConcurrency(int)"/>
    new IPostgresReceiveEndpointDescriptor MaxConcurrency(int maxConcurrency);

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
