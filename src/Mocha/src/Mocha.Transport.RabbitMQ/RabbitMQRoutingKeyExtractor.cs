namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Extracts a routing key from a message instance, stored as a feature on <see cref="MessageType"/>
/// to support transport-specific exchange routing.
/// </summary>
internal sealed class RabbitMQRoutingKeyExtractor(Func<object, string?> extractor)
{
    /// <summary>
    /// Extracts the routing key from the specified message.
    /// </summary>
    /// <param name="message">The message to extract the routing key from.</param>
    /// <returns>The routing key, or <c>null</c> if none could be determined.</returns>
    public string? Extract(object message) => extractor(message);
}
