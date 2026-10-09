namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Header keys used for RabbitMQ message properties.
/// </summary>
internal static class RabbitMQMessageHeaders
{
    /// <summary>
    /// Header key for the MIME content type of the serialized message body.
    /// </summary>
    public static readonly ContextDataKey<string> ContentType = new("x-content-type");

    /// <summary>
    /// Header key for the AMQP routing key, used to route messages to the correct exchange binding.
    /// </summary>
    public static readonly ContextDataKey<string> RoutingKey = new("x-routing-key");

    /// <summary>
    /// Header key for the delivery count maintained by RabbitMQ quorum queues.
    /// </summary>
    public static readonly ContextDataKey<long> DeliveryCount = new("x-delivery-count");
}
