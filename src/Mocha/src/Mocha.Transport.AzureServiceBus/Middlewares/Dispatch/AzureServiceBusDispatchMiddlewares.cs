namespace Mocha.Transport.AzureServiceBus.Middlewares;

/// <summary>
/// Provides pre-configured Azure Service Bus-specific dispatch middleware configurations.
/// </summary>
public static class AzureServiceBusDispatchMiddlewares
{
    /// <summary>
    /// Middleware configuration that runs the per-type extractors for Azure Service Bus native message
    /// properties (<c>SessionId</c>, <c>PartitionKey</c>, <c>ReplyToSessionId</c>, <c>To</c>) and writes
    /// their values to the dispatch headers.
    /// </summary>
    public static readonly DispatchMiddlewareConfiguration MessageProperties =
        AzureServiceBusMessagePropertiesMiddleware.Create();
}
