namespace Mocha.Inbox;

/// <summary>
/// Provides the inbox consumer middleware configuration. Use its key to place middleware relative to it.
/// </summary>
public static class InboxConsumerMiddlewares
{
    /// <summary>
    /// Skips messages that the current consumer has already processed.
    /// </summary>
    public static readonly ConsumerMiddlewareConfiguration Inbox = ConsumeInboxMiddleware.Create();
}
