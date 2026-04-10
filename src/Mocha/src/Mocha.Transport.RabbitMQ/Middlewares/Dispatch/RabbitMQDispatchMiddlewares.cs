namespace Mocha.Transport.RabbitMQ.Middlewares;

/// <summary>
/// Provides pre-configured RabbitMQ-specific dispatch middleware configurations.
/// </summary>
public static class RabbitMQDispatchMiddlewares
{
    /// <summary>
    /// Middleware configuration that extracts a routing key from the message and writes it to the dispatch headers.
    /// </summary>
    public static readonly DispatchMiddlewareConfiguration RoutingKey = RabbitMQRoutingKeyMiddleware.Create();
}
