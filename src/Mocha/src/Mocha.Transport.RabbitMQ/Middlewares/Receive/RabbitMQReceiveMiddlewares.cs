using Mocha.Middlewares;

namespace Mocha.Transport.RabbitMQ.Middlewares;

/// <summary>
/// Provides pre-configured RabbitMQ-specific receive middleware configurations for acknowledgement and message parsing.
/// </summary>
public static class RabbitMQReceiveMiddlewares
{
    /// <summary>
    /// Middleware configuration that acknowledges messages on success and negatively acknowledges (with requeue) on failure.
    /// </summary>
    public static readonly ReceiveMiddlewareConfiguration Acknowledgement = RabbitMQAcknowledgementMiddleware.Create();

    /// <summary>
    /// Middleware configuration that parses the raw RabbitMQ delivery into a <see cref="MessageEnvelope"/> on the receive context.
    /// </summary>
    public static readonly ReceiveMiddlewareConfiguration Parsing = RabbitMQParsingMiddleware.Create();
}
