namespace Mocha.Transport.Kafka;

/// <summary>
/// Well-known header keys used by the Kafka transport for message metadata.
/// </summary>
internal static class KafkaMessageHeaders
{
    public const string MessageId = "mocha-message-id";
    public const string CorrelationId = "mocha-correlation-id";
    public const string ConversationId = "mocha-conversation-id";
    public const string CausationId = "mocha-causation-id";
    public const string SourceAddress = "mocha-source-address";
    public const string DestinationAddress = "mocha-destination-address";
    public const string ResponseAddress = "mocha-response-address";
    public const string FaultAddress = "mocha-fault-address";
    public const string ContentType = "mocha-content-type";
    public const string MessageType = "mocha-message-type";
    public const string SentAt = "mocha-sent-at";
    public const string EnclosedMessageTypes = "mocha-enclosed-message-types";
}
