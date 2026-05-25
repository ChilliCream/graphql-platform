namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Extension methods for configuring RabbitMQ exchange descriptors with alternate exchange routing.
/// </summary>
public static class RabbitMQExchangeDescriptorExtensions
{
    /// <summary>
    /// Sets an alternate exchange for messages that cannot be routed.
    /// Messages that cannot be routed to any queue will be sent to the alternate exchange.
    /// </summary>
    /// <param name="descriptor">The exchange descriptor.</param>
    /// <param name="alternateExchangeName">The name of the alternate exchange.</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static IRabbitMQExchangeDescriptor AlternateExchange(
        this IRabbitMQExchangeDescriptor descriptor,
        string alternateExchangeName)
    {
        return descriptor.WithArgument("alternate-exchange", alternateExchangeName);
    }
}
