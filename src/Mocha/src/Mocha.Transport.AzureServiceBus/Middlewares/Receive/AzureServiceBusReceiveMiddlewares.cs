using Mocha.Middlewares;

namespace Mocha.Transport.AzureServiceBus.Middlewares;

/// <summary>
/// Provides pre-configured Azure Service Bus-specific receive middleware configurations
/// for acknowledgement and message parsing.
/// </summary>
public static class AzureServiceBusReceiveMiddlewares
{
    /// <summary>
    /// Middleware configuration that completes messages on success and abandons them on failure.
    /// </summary>
    public static readonly ReceiveMiddlewareConfiguration Acknowledgement =
        AzureServiceBusAcknowledgementMiddleware.Create();

    /// <summary>
    /// Middleware configuration that parses the raw Azure Service Bus message into a
    /// <see cref="MessageEnvelope"/> on the receive context.
    /// </summary>
    public static readonly ReceiveMiddlewareConfiguration Parsing =
        AzureServiceBusParsingMiddleware.Create();
}
