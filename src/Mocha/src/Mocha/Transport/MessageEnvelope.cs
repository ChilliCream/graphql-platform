using System.Collections.Immutable;

namespace Mocha.Middlewares;

/// <summary>
/// Wire-format envelope that wraps a serialized message body with transport metadata, correlation
/// identifiers, headers, and addressing information for cross-process messaging.
/// </summary>
/// <remarks>
/// All properties use init-only setters so envelopes are effectively immutable after construction.
/// The copy constructor creates a deep copy of mutable state (headers) while sharing the read-only body buffer.
/// </remarks>
public sealed class MessageEnvelope
{
    /// <summary>
    /// Creates an empty envelope with default values.
    /// </summary>
    public MessageEnvelope() { }

    /// <summary>
    /// Creates a deep copy of the specified envelope, cloning mutable headers while sharing
    /// the read-only body buffer.
    /// </summary>
    /// <param name="envelope">The source envelope to copy. Must not be <see langword="null"/>.</param>
    public MessageEnvelope(MessageEnvelope envelope)
    {
        MessageId = envelope.MessageId;
        CorrelationId = envelope.CorrelationId;
        ConversationId = envelope.ConversationId;
        CausationId = envelope.CausationId;
        SourceAddress = envelope.SourceAddress;
        DestinationAddress = envelope.DestinationAddress;
        ResponseAddress = envelope.ResponseAddress;
        FaultAddress = envelope.FaultAddress;
        ContentType = envelope.ContentType;
        MessageType = envelope.MessageType;
        SentAt = envelope.SentAt;
        DeliverBy = envelope.DeliverBy;
        DeliveryCount = envelope.DeliveryCount;
        Headers = envelope.Headers is not null ? new Headers(envelope.Headers) : null;
        Body = envelope.Body;
        EnclosedMessageTypes = envelope.EnclosedMessageTypes;
        Host = envelope.Host;
    }

    /// <summary>
    /// Unique identifier for the message.
    /// </summary>
    public string? MessageId { get; init; }

    /// <summary>
    /// Used to correlate a set of related messages (requests, workflows, sagas).
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// A larger conversation flow (multiple related correlation scopes).
    /// </summary>
    public string? ConversationId { get; init; }

    /// <summary>
    /// Parent message that triggered this one.
    /// </summary>
    public string? CausationId { get; init; }

    /// <summary>
    /// Address of the endpoint that originally dispatched this message.
    /// </summary>
    public string? SourceAddress { get; init; }

    /// <summary>
    /// Address of the endpoint this message is being delivered to.
    /// </summary>
    public string? DestinationAddress { get; init; }

    /// <summary>
    /// Address where replies to this message should be sent, enabling request/response patterns.
    /// </summary>
    public string? ResponseAddress { get; init; }

    // TODO this will only be used when we do faults, which is still a todo
    /// <summary>
    /// Address where fault notifications should be sent if the message cannot be processed.
    /// </summary>
    public string? FaultAddress { get; init; }

    /// <summary>
    /// MIME content type of the serialized <see cref="Body"/> (e.g., "application/json").
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// URN of the message type.
    /// </summary>
    public string? MessageType { get; init; }

    /// <summary>
    /// UTC timestamp when the envelope was created.
    /// </summary>
    public DateTimeOffset? SentAt { get; init; }

    /// <summary>
    /// Must be processed before this timestamp.
    /// Used for TTL / NServiceBus "TimeToBeReceived".
    /// </summary>
    public DateTimeOffset? DeliverBy { get; init; }

    /// <summary>
    /// Delivery attempt counter.
    /// </summary>
    public int? DeliveryCount { get; init; }

    /// <summary>
    /// User-defined and infrastructure headers.
    /// </summary>
    public IHeaders? Headers { get; init; }

    /// <summary>
    /// Raw message body (serializer defined by bus configuration).
    /// </summary>
    public ReadOnlyMemory<byte> Body { get; init; } = Array.Empty<byte>();

    /// <summary>
    /// The list of message type URNs enclosed in this envelope, supporting polymorphic deserialization
    /// when a message implements multiple contracts.
    /// </summary>
    public ImmutableArray<string>? EnclosedMessageTypes { get; init; }

    /// <summary>
    /// Information about the remote host that produced this message (machine name, process ID, etc.).
    /// </summary>
    public IRemoteHostInfo? Host { get; init; }

    /// <summary>
    /// Well-known property name constants used for serialization and header mapping of envelope fields.
    /// </summary>
    public static class Properties
    {
        /// <summary>Property name for <see cref="MessageEnvelope.MessageId"/>.</summary>
        public const string MessageId = "messageId";

        /// <summary>Property name for <see cref="MessageEnvelope.CorrelationId"/>.</summary>
        public const string CorrelationId = "correlationId";

        /// <summary>Property name for <see cref="MessageEnvelope.ConversationId"/>.</summary>
        public const string ConversationId = "conversationId";

        /// <summary>Property name for <see cref="MessageEnvelope.CausationId"/>.</summary>
        public const string CausationId = "causationId";

        /// <summary>Property name for <see cref="MessageEnvelope.SourceAddress"/>.</summary>
        public const string SourceAddress = "sourceAddress";

        /// <summary>Property name for <see cref="MessageEnvelope.DestinationAddress"/>.</summary>
        public const string DestinationAddress = "destinationAddress";

        /// <summary>Property name for <see cref="MessageEnvelope.ResponseAddress"/>.</summary>
        public const string ResponseAddress = "responseAddress";

        /// <summary>Property name for <see cref="MessageEnvelope.FaultAddress"/>.</summary>
        public const string FaultAddress = "faultAddress";

        /// <summary>Property name for <see cref="MessageEnvelope.ContentType"/>.</summary>
        public const string ContentType = "contentType";

        /// <summary>Property name for <see cref="MessageEnvelope.MessageType"/>.</summary>
        public const string MessageType = "messageType";

        /// <summary>Property name for <see cref="MessageEnvelope.SentAt"/>.</summary>
        public const string SentAt = "sentAt";

        /// <summary>Property name for <see cref="MessageEnvelope.DeliverBy"/>.</summary>
        public const string DeliverBy = "deliverBy";

        /// <summary>Property name for <see cref="MessageEnvelope.DeliveryCount"/>.</summary>
        public const string DeliveryCount = "deliveryCount";

        /// <summary>Property name for <see cref="MessageEnvelope.Headers"/>.</summary>
        public const string Headers = "headers";

        /// <summary>Property name for <see cref="MessageEnvelope.Body"/>.</summary>
        public const string Body = "body";

        /// <summary>Property name for <see cref="MessageEnvelope.EnclosedMessageTypes"/>.</summary>
        public const string EnclosedMessageTypes = "enclosedMessageTypes";

        /// <summary>Property name for <see cref="MessageEnvelope.Host"/>.</summary>
        public const string Host = "host";
    }
}
