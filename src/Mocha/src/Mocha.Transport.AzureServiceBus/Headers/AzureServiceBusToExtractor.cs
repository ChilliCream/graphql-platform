namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Extracts a <c>To</c> address from a message instance, stored as a feature on <see cref="MessageType"/>
/// for autoforward chaining. Reserved by the broker and not currently used for routing.
/// </summary>
internal sealed class AzureServiceBusToExtractor(Func<object, string?> extractor)
{
    /// <summary>
    /// Extracts the logical to-address from the specified message.
    /// </summary>
    /// <param name="message">The message to extract the to-address from.</param>
    /// <returns>The to-address, or <c>null</c> if none could be determined.</returns>
    public string? Extract(object message) => extractor(message);

    public static AzureServiceBusToExtractor Create<TMessage>(Func<TMessage, string?> extractor)
    {
        return new AzureServiceBusToExtractor(msg => extractor((TMessage)msg));
    }
}
