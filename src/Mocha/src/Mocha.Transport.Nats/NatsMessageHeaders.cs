namespace Mocha.Transport.Nats;

/// <summary>
/// Header keys used to carry a <see cref="Mocha.Middlewares.MessageEnvelope"/> over NATS.
/// </summary>
/// <remarks>
/// NATS has no native message properties, so every envelope field travels as a header. The
/// <c>x-</c> keys match the ones used by the RabbitMQ transport; fields that RabbitMQ maps onto
/// AMQP basic properties need keys of their own here.
/// </remarks>
internal static class NatsMessageHeaders
{
    /// <summary>
    /// The JetStream deduplication key, which is qualified by destination subject and is not the
    /// message identifier. The identifier travels in <see cref="MessageId"/>.
    /// </summary>
    // Deduplication is scoped to the stream rather than to the subject, so republishing an envelope
    // to a second subject in the same stream under its own identifier would be discarded as a
    // duplicate. Dead-lettering to an error subject does exactly that, hence the qualification.
    public const string DeduplicationKey = "Nats-Msg-Id";

    /// <summary>
    /// Header key for the message identifier.
    /// </summary>
    public const string MessageId = "x-message-id";

    /// <summary>
    /// Header key for the correlation identifier.
    /// </summary>
    public const string CorrelationId = "x-correlation-id";

    /// <summary>
    /// Header key for the conversation identifier that correlates a group of causally related messages.
    /// </summary>
    public const string ConversationId = "x-conversation-id";

    /// <summary>
    /// Header key for the causation identifier linking a message to the command or event that triggered it.
    /// </summary>
    public const string CausationId = "x-causation-id";

    /// <summary>
    /// Header key for the originating endpoint address of the message.
    /// </summary>
    public const string SourceAddress = "x-source-address";

    /// <summary>
    /// Header key for the intended destination endpoint address of the message.
    /// </summary>
    public const string DestinationAddress = "x-destination-address";

    /// <summary>
    /// Header key for the endpoint address replies should be sent to.
    /// </summary>
    public const string ResponseAddress = "x-response-address";

    /// <summary>
    /// Header key for the endpoint address where fault messages should be sent on processing failure.
    /// </summary>
    public const string FaultAddress = "x-fault-address";

    /// <summary>
    /// Header key for the fully qualified type name of the message payload.
    /// </summary>
    public const string MessageType = "x-message-type";

    /// <summary>
    /// Header key for the MIME content type of the serialized message body.
    /// </summary>
    public const string ContentType = "x-content-type";

    /// <summary>
    /// Header key for the list of message type names enclosed in the envelope, used for polymorphic deserialization.
    /// </summary>
    public const string EnclosedMessageTypes = "x-enclosed-message-types";

    /// <summary>
    /// Header key for the instant the message was sent, in round-trip format.
    /// </summary>
    public const string SentAt = "x-sent-at";

    /// <summary>
    /// Header key for the instant after which the message should no longer be delivered.
    /// </summary>
    public const string DeliverBy = "x-deliver-by";

    /// <summary>
    /// Header key for the instant a scheduled message becomes due.
    /// </summary>
    public const string ScheduledTime = "x-scheduled-time";

    private static readonly string[] s_reserved =
    [
        DeduplicationKey,
        MessageId,
        CorrelationId,
        ConversationId,
        CausationId,
        SourceAddress,
        DestinationAddress,
        ResponseAddress,
        FaultAddress,
        MessageType,
        ContentType,
        EnclosedMessageTypes,
        SentAt,
        DeliverBy,
        ScheduledTime
    ];

    /// <summary>
    /// Determines whether the specified header key is owned by the transport and therefore
    /// excluded when rebuilding the user-defined headers of an envelope.
    /// </summary>
    /// <param name="key">The header key to test.</param>
    /// <returns><see langword="true"/> when the key is transport-owned; otherwise <see langword="false"/>.</returns>
    public static bool IsReserved(string key)
    {
        foreach (var reserved in s_reserved)
        {
            if (string.Equals(key, reserved, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return key.StartsWith("Nats-", StringComparison.OrdinalIgnoreCase);
    }
}
