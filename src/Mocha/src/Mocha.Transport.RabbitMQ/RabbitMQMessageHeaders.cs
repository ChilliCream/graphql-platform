using System.Collections.Immutable;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Header keys used for RabbitMQ message properties.
/// </summary>
internal static class RabbitMQMessageHeaders
{
    /// <summary>
    /// Header key for the conversation identifier that correlates a group of causally related messages.
    /// </summary>
    public static readonly ContextDataKey<string> ConversationId = new("x-conversation-id");

    /// <summary>
    /// Header key for the causation identifier linking a message to the command or event that triggered it.
    /// </summary>
    public static readonly ContextDataKey<string> CausationId = new("x-causation-id");

    /// <summary>
    /// Header key for the originating endpoint address of the message.
    /// </summary>
    public static readonly ContextDataKey<string> SourceAddress = new("x-source-address");

    /// <summary>
    /// Header key for the intended destination endpoint address of the message.
    /// </summary>
    public static readonly ContextDataKey<string> DestinationAddress = new("x-destination-address");

    /// <summary>
    /// Header key for the endpoint address where fault messages should be sent on processing failure.
    /// </summary>
    public static readonly ContextDataKey<string> FaultAddress = new("x-fault-address");

    /// <summary>
    /// Header key for the fully qualified type name of the message payload.
    /// </summary>
    public static readonly ContextDataKey<string> MessageType = new("x-message-type");

    /// <summary>
    /// Header key for the MIME content type of the serialized message body.
    /// </summary>
    public static readonly ContextDataKey<string> ContentType = new("x-content-type");

    /// <summary>
    /// Header key for the AMQP routing key, used to route messages to the correct exchange binding.
    /// </summary>
    public static readonly ContextDataKey<string> RoutingKey = new("x-routing-key");

    /// <summary>
    /// Header key for the list of message type names enclosed in the envelope, used for polymorphic deserialization.
    /// </summary>
    public static readonly ContextDataKey<ImmutableArray<string>> EnclosedMessageTypes = new(
        "x-enclosed-message-types");

    /// <summary>
    /// Header key for the delivery count maintained by RabbitMQ quorum queues.
    /// </summary>
    public static readonly ContextDataKey<long> DeliveryCount = new("x-delivery-count");
}
