namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Extracts a <c>SessionId</c> from a message instance, stored as a feature on <see cref="MessageType"/>
/// to support session-aware queues and subscriptions.
/// </summary>
internal sealed class AzureServiceBusSessionIdExtractor(Func<object, string?> extractor)
{
    /// <summary>
    /// Extracts the session identifier from the specified message.
    /// </summary>
    /// <param name="message">The message to extract the session identifier from.</param>
    /// <returns>The session identifier, or <c>null</c> if none could be determined.</returns>
    public string? Extract(object message) => extractor(message);

    public static AzureServiceBusSessionIdExtractor Create<TMessage>(Func<TMessage, string?> extractor)
    {
        return new AzureServiceBusSessionIdExtractor(msg => extractor((TMessage)msg));
    }
}
