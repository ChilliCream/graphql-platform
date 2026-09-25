using Mocha.Middlewares;

namespace Mocha.Transport.Nats.Middlewares;

/// <summary>
/// The receive middlewares contributed by the NATS transport.
/// </summary>
public static class NatsReceiveMiddlewares
{
    /// <summary>
    /// Settles each message according to how the pipeline finished.
    /// </summary>
    public static readonly ReceiveMiddlewareConfiguration Acknowledgement =
        NatsAcknowledgementMiddleware.Create();

    /// <summary>
    /// Turns the raw JetStream message into a message envelope.
    /// </summary>
    public static readonly ReceiveMiddlewareConfiguration Parsing = NatsParsingMiddleware.Create();
}
