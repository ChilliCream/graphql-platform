namespace Mocha.Inbox;

/// <summary>
/// Provides the consumer middleware configuration that the inbox adds to the consumer pipeline.
/// </summary>
public static class InboxConsumerMiddlewares
{
    /// <summary>
    /// The inbox middleware configuration that skips messages the current consumer has already processed.
    /// </summary>
    public static readonly ConsumerMiddlewareConfiguration Inbox = ConsumeInboxMiddleware.Create();
}
