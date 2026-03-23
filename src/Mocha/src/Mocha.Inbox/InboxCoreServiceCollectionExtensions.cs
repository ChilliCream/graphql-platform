namespace Mocha.Inbox;

/// <summary>
/// Provides extension methods to register inbox infrastructure on <see cref="IMessageBusHostBuilder"/>.
/// </summary>
public static class InboxCoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers the core inbox services and inserts the inbox consumer middleware into the message bus pipeline.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The inbox middleware runs in the <b>consumer</b> pipeline so that the inbox claim
    /// participates in the same database transaction as the handler's side-effects (when
    /// <c>UseTransaction()</c> is configured). This makes the claim and the handler's
    /// business data atomic: both commit or both rollback.
    /// </para>
    /// <para>
    /// The middleware is appended to the consumer pipeline so that it runs after any
    /// transaction middleware that may have been registered.
    /// </para>
    /// </remarks>
    /// <param name="builder">The message bus host builder to configure.</param>
    /// <returns>The same <paramref name="builder"/> instance for chaining.</returns>
    public static IMessageBusHostBuilder UseInboxCore(this IMessageBusHostBuilder builder)
    {
        builder.ConfigureMessageBus(x => x.UseConsume(ConsumeInboxMiddleware.Create()));

        return builder;
    }
}
