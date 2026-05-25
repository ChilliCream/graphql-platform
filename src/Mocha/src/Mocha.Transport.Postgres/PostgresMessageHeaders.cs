namespace Mocha.Transport.Postgres;

/// <summary>
/// Header keys used for PostgreSQL message properties stored as JSON.
/// </summary>
internal static class PostgresMessageHeaders
{
    /// <summary>
    /// Header key for the unique message identifier.
    /// </summary>
    public static ReadOnlySpan<byte> MessageId => "messageId"u8;

    /// <summary>
    /// Header key for the correlation identifier.
    /// </summary>
    public static ReadOnlySpan<byte> CorrelationId => "correlationId"u8;

    /// <summary>
    /// Header key for the conversation identifier that correlates a group of causally related messages.
    /// </summary>
    public static ReadOnlySpan<byte> ConversationId => "conversationId"u8;

    /// <summary>
    /// Header key for the causation identifier linking a message to the command or event that triggered it.
    /// </summary>
    public static ReadOnlySpan<byte> CausationId => "causationId"u8;

    /// <summary>
    /// Header key for the originating endpoint address of the message.
    /// </summary>
    public static ReadOnlySpan<byte> SourceAddress => "sourceAddress"u8;

    /// <summary>
    /// Header key for the intended destination endpoint address of the message.
    /// </summary>
    public static ReadOnlySpan<byte> DestinationAddress => "destinationAddress"u8;

    /// <summary>
    /// Header key for the response address for request/reply patterns.
    /// </summary>
    public static ReadOnlySpan<byte> ResponseAddress => "responseAddress"u8;

    /// <summary>
    /// Header key for the endpoint address where fault messages should be sent on processing failure.
    /// </summary>
    public static ReadOnlySpan<byte> FaultAddress => "faultAddress"u8;

    /// <summary>
    /// Header key for the MIME content type of the serialized message body.
    /// </summary>
    public static ReadOnlySpan<byte> ContentType => "contentType"u8;

    /// <summary>
    /// Header key for the fully qualified type name of the message payload.
    /// </summary>
    public static ReadOnlySpan<byte> MessageType => "messageType"u8;

    /// <summary>
    /// Header key for the list of message type names enclosed in the envelope, used for polymorphic deserialization.
    /// </summary>
    public static ReadOnlySpan<byte> EnclosedMessageTypes => "enclosedMessageTypes"u8;

    /// <summary>
    /// Header key for the deadline by which the message must be delivered and processed.
    /// </summary>
    public static ReadOnlySpan<byte> DeliverBy => "deliverBy"u8;

    /// <summary>
    /// Header key for the earliest time at which the message should be made available for consumption.
    /// </summary>
    public static ReadOnlySpan<byte> ScheduledTime => "scheduledTime"u8;
}
