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

    /// <summary>
    /// Header key for the time the message was sent, stored as Unix milliseconds.
    /// </summary>
    public static readonly ContextDataKey<long> SentAt = new("x-sent-at");

    /// <summary>
    /// Header key for the time by which the message must be delivered, stored as Unix milliseconds.
    /// </summary>
    public static readonly ContextDataKey<long> DeliverBy = new("x-deliver-by");

    /// <summary>
    /// Returns whether the header carries an envelope property and is therefore not a message header.
    /// </summary>
    public static bool IsEnvelopeProperty(string key) => key == SentAt.Key || key == DeliverBy.Key;
}
