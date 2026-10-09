namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Extracts a <c>ReplyToSessionId</c> from a message instance, stored as a feature on
/// <see cref="MessageType"/> to support multiplexed request/reply over session-aware reply queues.
/// </summary>
internal sealed class AzureServiceBusReplyToSessionIdExtractor(Func<object, string?> extractor)
{
    /// <summary>
    /// Extracts the reply-to session identifier from the specified message.
    /// </summary>
    /// <param name="message">The message to extract the reply-to session identifier from.</param>
    /// <returns>The reply-to session identifier, or <c>null</c> if none could be determined.</returns>
    public string? Extract(object message) => extractor(message);

    public static AzureServiceBusReplyToSessionIdExtractor Create<TMessage>(Func<TMessage, string?> extractor)
    {
        return new AzureServiceBusReplyToSessionIdExtractor(msg => extractor((TMessage)msg));
    }
}
