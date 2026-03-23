namespace Mocha.Transport.Postgres;

/// <summary>
/// Fluent interface for configuring a PostgreSQL dispatch endpoint, including its target
/// destination and dispatch middleware pipeline.
/// </summary>
public interface IPostgresDispatchEndpointDescriptor
    : IDispatchEndpointDescriptor<PostgresDispatchEndpointConfiguration>
{
    /// <summary>
    /// Directs this endpoint to dispatch messages to the specified PostgreSQL queue.
    /// </summary>
    /// <param name="name">The name of the target queue.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IPostgresDispatchEndpointDescriptor ToQueue(string name);

    /// <summary>
    /// Directs this endpoint to dispatch messages to the specified PostgreSQL topic.
    /// </summary>
    /// <param name="name">The name of the target topic.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IPostgresDispatchEndpointDescriptor ToTopic(string name);

    /// <inheritdoc cref="IDispatchEndpointDescriptor{T}.Send{TMessage}"/>
    new IPostgresDispatchEndpointDescriptor Send<TMessage>();

    /// <inheritdoc cref="IDispatchEndpointDescriptor{T}.Publish{TMessage}"/>
    new IPostgresDispatchEndpointDescriptor Publish<TMessage>();

    /// <inheritdoc cref="IDispatchEndpointDescriptor{T}.UseDispatch(DispatchMiddlewareConfiguration, string?, string?)"/>
    new IPostgresDispatchEndpointDescriptor UseDispatch(
        DispatchMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null);
}
