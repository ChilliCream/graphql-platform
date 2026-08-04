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
    /// Adds a dispatch middleware to this endpoint's dispatch pipeline. Optionally positions it
    /// relative to an existing middleware by specifying <paramref name="before"/> or <paramref name="after"/>.
    /// </summary>
    /// <param name="configuration">The dispatch middleware configuration to add.</param>
    /// <param name="before">The name of the existing middleware to insert before.</param>
    /// <param name="after">The name of the existing middleware to insert after.</param>
    /// <returns>The descriptor instance for method chaining.</returns>
    IDispatchEndpointDescriptor<TConfiguration> UseDispatch(
        DispatchMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null);
}
