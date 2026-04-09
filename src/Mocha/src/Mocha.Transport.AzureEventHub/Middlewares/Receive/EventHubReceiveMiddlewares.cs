using Mocha.Middlewares;

namespace Mocha.Transport.AzureEventHub.Middlewares;

/// <summary>
/// Provides pre-configured Event Hub-specific receive middleware configurations for acknowledgement and message parsing.
/// </summary>
public static class EventHubReceiveMiddlewares
{
    /// <summary>
    /// Middleware configuration for Event Hub acknowledgement (pass-through; checkpoint tracking is done by the processor).
    /// </summary>
    public static readonly ReceiveMiddlewareConfiguration Acknowledgement = EventHubAcknowledgementMiddleware.Create();

    /// <summary>
    /// Middleware configuration that parses the raw Event Hub event into a <see cref="MessageEnvelope"/> on the receive context.
    /// </summary>
    public static readonly ReceiveMiddlewareConfiguration Parsing = EventHubParsingMiddleware.Create();
}
