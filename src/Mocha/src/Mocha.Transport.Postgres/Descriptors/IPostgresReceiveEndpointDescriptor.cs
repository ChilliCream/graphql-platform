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

    /// <inheritdoc cref="IReceiveEndpointDescriptor{T}.Receives(Type)"/>
    new IPostgresReceiveEndpointDescriptor Receives(Type messageType);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{T}.BindImplicitly"/>
    new IPostgresReceiveEndpointDescriptor BindImplicitly();

    /// <inheritdoc cref="IReceiveEndpointDescriptor{T}.BindExplicitly"/>
    new IPostgresReceiveEndpointDescriptor BindExplicitly();

    /// <inheritdoc cref="IReceiveEndpointDescriptor{T}.Kind(ReceiveEndpointKind)"/>
    new IPostgresReceiveEndpointDescriptor Kind(ReceiveEndpointKind kind);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{T}.MaxConcurrency(int)"/>
    new IPostgresReceiveEndpointDescriptor MaxConcurrency(int maxConcurrency);

    /// <summary>
    /// Sets the address of the fault endpoint where failed messages are forwarded.
    /// </summary>
    /// <param name="name">The fault endpoint address.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IPostgresReceiveEndpointDescriptor FaultEndpoint(string name);

    /// <summary>
    /// Sets the address of the endpoint where skipped messages are forwarded.
    /// </summary>
    /// <param name="name">The skipped endpoint address.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IPostgresReceiveEndpointDescriptor SkippedEndpoint(string name);

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
