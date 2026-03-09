namespace Mocha;

/// <summary>
/// Describes the configuration surface for a dispatch endpoint, including outbound route bindings and dispatch middleware.
/// </summary>
/// <typeparam name="TConfiguration">The dispatch endpoint configuration type.</typeparam>
public interface IDispatchEndpointDescriptor<TConfiguration> : IMessagingDescriptor<TConfiguration>
    where TConfiguration : DispatchEndpointConfiguration
{
    /// <summary>
    /// Binds a send route for the specified message type to this dispatch endpoint.
    /// </summary>
    /// <typeparam name="TMessage">The event type to send through this endpoint.</typeparam>
    /// <returns>The descriptor instance for method chaining.</returns>
    IDispatchEndpointDescriptor<TConfiguration> Send<TMessage>();

    /// <summary>
    /// Binds a publish route for the specified message type to this dispatch endpoint.
    /// </summary>
    /// <typeparam name="TMessage">The event type to publish through this endpoint.</typeparam>
    /// <returns>The descriptor instance for method chaining.</returns>
    IDispatchEndpointDescriptor<TConfiguration> Publish<TMessage>();

    /// <summary>
    /// Appends a dispatch middleware configuration to this endpoint's dispatch pipeline.
    /// </summary>
    /// <param name="configuration">The dispatch middleware configuration to add.</param>
    /// <returns>The descriptor instance for method chaining.</returns>
    IDispatchEndpointDescriptor<TConfiguration> UseDispatch(DispatchMiddlewareConfiguration configuration);

    /// <summary>
    /// Inserts a dispatch middleware configuration after the middleware with the specified name.
    /// </summary>
    /// <param name="after">The name of the existing middleware after which to insert.</param>
    /// <param name="configuration">The dispatch middleware configuration to insert.</param>
    /// <returns>The descriptor instance for method chaining.</returns>
    IDispatchEndpointDescriptor<TConfiguration> AppendDispatch(
        string after,
        DispatchMiddlewareConfiguration configuration);

    /// <summary>
    /// Inserts a dispatch middleware configuration before the middleware with the specified name.
    /// </summary>
    /// <param name="before">The name of the existing middleware before which to insert.</param>
    /// <param name="configuration">The dispatch middleware configuration to insert.</param>
    /// <returns>The descriptor instance for method chaining.</returns>
    IDispatchEndpointDescriptor<TConfiguration> PrependDispatch(
        string before,
        DispatchMiddlewareConfiguration configuration);
}
