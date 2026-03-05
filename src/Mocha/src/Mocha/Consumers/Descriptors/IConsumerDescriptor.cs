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
    /// Appends a consumer-scoped middleware configuration to the consumer's middleware pipeline.
    /// </summary>
    /// <param name="configuration">The consumer middleware configuration to add.</param>
    /// <returns>The descriptor instance for method chaining.</returns>
    IConsumerDescriptor UseConsumer(ConsumerMiddlewareConfiguration configuration);

    /// <summary>
    /// Inserts a consumer-scoped middleware configuration immediately after the middleware with the
    /// specified name.
    /// </summary>
    /// <param name="after">The name of the existing middleware after which to insert.</param>
    /// <param name="configuration">The consumer middleware configuration to insert.</param>
    /// <returns>The descriptor instance for method chaining.</returns>
    IConsumerDescriptor AppendConsumer(string after, ConsumerMiddlewareConfiguration configuration);

    /// <summary>
    /// Inserts a consumer-scoped middleware configuration immediately before the middleware with
    /// the specified name.
    /// </summary>
    /// <param name="before">The name of the existing middleware before which to insert.</param>
    /// <param name="configuration">The consumer middleware configuration to insert.</param>
    /// <returns>The descriptor instance for method chaining.</returns>
    IConsumerDescriptor PrependConsumer(string before, ConsumerMiddlewareConfiguration configuration);
}
