namespace Mocha;

/// <summary>
/// Describes the configuration surface for a receive endpoint, including consumer bindings, error handling, concurrency, and receive middleware.
/// </summary>
/// <typeparam name="TConfiguration">The receive endpoint configuration type.</typeparam>
public interface IReceiveEndpointDescriptor<out TConfiguration>
    : IMessagingDescriptor<TConfiguration>
    , IReceiveMiddlewareProvider where TConfiguration : ReceiveEndpointConfiguration
{
    /// <summary>
    /// Binds a handler to this receive endpoint, ensuring its messages are consumed on this endpoint.
    /// </summary>
    /// <typeparam name="THandler">The handler type implementing <see cref="IHandler"/>.</typeparam>
    /// <returns>The descriptor instance for method chaining.</returns>
    IReceiveEndpointDescriptor<TConfiguration> Handler<THandler>() where THandler : class, IHandler;

    /// <summary>
    /// Binds a handler to this receive endpoint by its runtime type.
    /// </summary>
    /// <param name="handlerType">The handler type to bind.</param>
    /// <returns>The descriptor instance for method chaining.</returns>
    IReceiveEndpointDescriptor<TConfiguration> Handler(Type handlerType);

    /// <summary>
    /// Binds a consumer to this receive endpoint by its runtime type.
    /// </summary>
    /// <param name="consumerType">The consumer type to bind.</param>
    /// <returns>The descriptor instance for method chaining.</returns>
    IReceiveEndpointDescriptor<TConfiguration> Consumer(Type consumerType);

    /// <summary>
    /// Binds a consumer to this receive endpoint, ensuring its messages are consumed on this
    /// endpoint.
    /// </summary>
    /// <typeparam name="TConsumer">The consumer type implementing <see cref="IConsumer"/>.</typeparam>
    /// <returns>The descriptor instance for method chaining.</returns>
    IReceiveEndpointDescriptor<TConfiguration> Consumer<TConsumer>() where TConsumer : class, IConsumer;

    /// <summary>
    /// Sets the kind of this receive endpoint (e.g., default, temporary).
    /// </summary>
    /// <param name="kind">The receive endpoint kind.</param>
    /// <returns>The descriptor instance for method chaining.</returns>
    IReceiveEndpointDescriptor<TConfiguration> Kind(ReceiveEndpointKind kind);

    /// <summary>
    /// Sets the maximum number of messages that can be processed concurrently on this endpoint.
    /// </summary>
    /// <param name="maxConcurrency">The maximum concurrency level.</param>
    /// <returns>The descriptor instance for method chaining.</returns>
    IReceiveEndpointDescriptor<TConfiguration> MaxConcurrency(int maxConcurrency);

    /// <summary>
    /// Sets the address of the fault endpoint where failed messages are forwarded.
    /// </summary>
    /// <param name="name">The fault endpoint address.</param>
    /// <returns>The descriptor instance for method chaining.</returns>
    IReceiveEndpointDescriptor<TConfiguration> FaultEndpoint(string name);

    /// <summary>
    /// Sets the address of the endpoint where skipped (unroutable) messages are forwarded.
    /// </summary>
    /// <param name="name">The skipped endpoint address.</param>
    /// <returns>The descriptor instance for method chaining.</returns>
    IReceiveEndpointDescriptor<TConfiguration> SkippedEndpoint(string name);

    /// <summary>
    /// Adds a receive middleware to this endpoint's receive pipeline. Optionally positions it
    /// relative to an existing middleware by specifying <paramref name="before"/> or <paramref name="after"/>.
    /// </summary>
    /// <param name="configuration">The receive middleware configuration to add.</param>
    /// <param name="before">The name of the existing middleware to insert before.</param>
    /// <param name="after">The name of the existing middleware to insert after.</param>
    /// <returns>The descriptor instance for method chaining.</returns>
    IReceiveEndpointDescriptor<TConfiguration> UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null);
}
