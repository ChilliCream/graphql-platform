namespace Mocha.Outbox;

/// <summary>
/// Provides the outbox dispatch middleware configuration. Use its key to place middleware relative to it.
/// </summary>
public static class OutboxDispatchMiddlewares
{
    /// <summary>
    /// Persists outgoing messages in the outbox instead of sending them to the transport.
    /// </summary>
    public static readonly DispatchMiddlewareConfiguration Outbox = DispatchOutboxMiddleware.Create();
}
