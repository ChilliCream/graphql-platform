namespace Mocha;

/// <summary>
/// Describes the configuration surface for a consumer, including its name, inbound routes, and
/// consumer-scoped middleware.
/// </summary>
public interface IConsumerDescriptor : IMessagingDescriptor<ConsumerConfiguration>
{
    /// <summary>
    /// Sets the logical name of this consumer.
    /// </summary>
    /// <param name="name">The unique consumer name.</param>
    /// <returns>The descriptor instance for method chaining.</returns>
    IConsumerDescriptor Name(string name);

    /// <summary>
    /// Adds an inbound route that binds this consumer to a specific message type and routing
    /// pattern.
    /// </summary>
    /// <param name="configure">An action to configure the inbound route descriptor.</param>
    /// <returns>The descriptor instance for method chaining.</returns>
    IConsumerDescriptor AddRoute(Action<IInboundRouteDescriptor> configure);

    /// <summary>
    /// Adds a consumer-scoped middleware configuration to the consumer's middleware pipeline.
    /// When neither <paramref name="before"/> nor <paramref name="after"/> is specified, the
    /// middleware is appended to the end of the pipeline.
    /// When <paramref name="before"/> is specified, the middleware is inserted immediately before
    /// the middleware with the given key.
    /// When <paramref name="after"/> is specified, the middleware is inserted immediately after
    /// the middleware with the given key.
    /// </summary>
    /// <param name="configuration">The consumer middleware configuration to add.</param>
    /// <param name="before">
    /// The key of the existing middleware before which to insert, or <c>null</c> to skip
    /// positional insertion.
    /// </param>
    /// <param name="after">
    /// The key of the existing middleware after which to insert, or <c>null</c> to skip
    /// positional insertion.
    /// </param>
    /// <returns>The descriptor instance for method chaining.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when both <paramref name="before"/> and <paramref name="after"/> are specified.
    /// </exception>
    IConsumerDescriptor UseConsumer(
        ConsumerMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null);
}
