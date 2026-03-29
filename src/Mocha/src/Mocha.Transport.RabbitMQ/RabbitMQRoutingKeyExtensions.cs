namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Extension methods for configuring RabbitMQ routing keys on message type descriptors.
/// </summary>
public static class RabbitMQRoutingKeyExtensions
{
    /// <summary>
    /// Configures a routing key extractor for this message type, used when publishing to RabbitMQ exchanges.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="descriptor">The message type descriptor.</param>
    /// <param name="extractor">A function that extracts the routing key from a message instance.</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static IMessageTypeDescriptor UseRabbitMQRoutingKey<TMessage>(
        this IMessageTypeDescriptor descriptor,
        Func<TMessage, string?> extractor)
    {
        var features = descriptor.Extend().Configuration.Features;

        features.Set(RabbitMQRoutingKeyExtractor.Create(extractor));

        return descriptor;
    }
}
