namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Extracts a <c>PartitionKey</c> from a message instance, stored as a feature on <see cref="MessageType"/>
/// to support partitioned queues and topics.
/// </summary>
internal sealed class AzureServiceBusPartitionKeyExtractor(Func<object, string?> extractor)
{
    /// <summary>
    /// Extracts the partition key from the specified message.
    /// </summary>
    /// <param name="message">The message to extract the partition key from.</param>
    /// <returns>The partition key, or <c>null</c> if none could be determined.</returns>
    public string? Extract(object message) => extractor(message);

    public static AzureServiceBusPartitionKeyExtractor Create<TMessage>(Func<TMessage, string?> extractor)
    {
        return new AzureServiceBusPartitionKeyExtractor(msg => extractor((TMessage)msg));
    }
}
