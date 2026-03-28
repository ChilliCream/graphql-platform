using Mocha.Middlewares;

namespace Mocha.Transport.NATS.Middlewares;

/// <summary>
/// Provides pre-configured NATS-specific receive middleware configurations for acknowledgement and message parsing.
/// </summary>
public static class NatsReceiveMiddlewares
{
    /// <summary>
    /// Middleware configuration that acknowledges messages on success, negatively acknowledges on failure,
    /// and terminates delivery on poison messages that exceed the maximum retry count.
    /// </summary>
    public static readonly ReceiveMiddlewareConfiguration Acknowledgement = NatsAcknowledgementMiddleware.Create();

    /// <summary>
    /// Middleware configuration that parses the raw JetStream message into a <see cref="MessageEnvelope"/> on the receive context.
    /// </summary>
    public static readonly ReceiveMiddlewareConfiguration Parsing = NatsParsingMiddleware.Create();
}
