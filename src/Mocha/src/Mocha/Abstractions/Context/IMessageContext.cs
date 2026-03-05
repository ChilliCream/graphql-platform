using System.Collections.Immutable;
using Mocha.Middlewares;

namespace Mocha;

/// <summary>
/// Represents the context of a message.
/// </summary>
public interface IMessageContext : IFeatureProvider
{
    /// <summary>
    /// Gets or sets the transport from which this message was received.
    /// </summary>
    MessagingTransport Transport { get; set; }

    /// <summary>
    /// Gets or sets the receive endpoint that accepted this message.
    /// </summary>
    ReceiveEndpoint Endpoint { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier for this message.
    /// </summary>
    string? MessageId { get; set; }

    /// <summary>
    /// Gets or sets the correlation identifier used to group related messages in a workflow.
    /// </summary>
    string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the conversation identifier that tracks a logical conversation across multiple
    /// messages.
    /// </summary>
    string? ConversationId { get; set; }

    /// <summary>
    /// Gets or sets the causation identifier linking this message to the message that caused it.
    /// </summary>
    string? CausationId { get; set; }

    /// <summary>
    /// Gets or sets the address of the originating endpoint.
    /// </summary>
    Uri? SourceAddress { get; set; }

    /// <summary>
    /// Gets or sets the address of the endpoint that received this message.
    /// </summary>
    Uri? DestinationAddress { get; set; }

    /// <summary>
    /// Gets or sets the address where replies to this message should be sent.
    /// </summary>
    Uri? ResponseAddress { get; set; }

    /// <summary>
    /// Gets or sets the address where fault notifications for this message should be sent.
    /// </summary>
    Uri? FaultAddress { get; set; }

    /// <summary>
    /// Gets or sets the content type that describes the serialization format of the message body.
    /// </summary>
    MessageContentType? ContentType { get; set; }

    /// <summary>
    /// Gets or sets the message type descriptor resolved from the incoming message metadata.
    /// </summary>
    MessageType? MessageType { get; set; }

    /// <summary>
    /// Gets the read-only header collection associated with this message.
    /// </summary>
    IReadOnlyHeaders Headers { get; }

    /// <summary>
    /// Gets or sets the timestamp indicating when the message was sent.
    /// </summary>
    DateTimeOffset? SentAt { get; set; }

    /// <summary>
    /// Gets or sets the optional deadline by which the message must be delivered before it is
    /// considered expired.
    /// </summary>
    DateTimeOffset? DeliverBy { get; set; }

    /// <summary>
    /// Gets or sets the number of times delivery of this message has been attempted.
    /// </summary>
    int? DeliveryCount { get; set; }

    /// <summary>
    /// Gets the raw serialized message body as a read-only byte buffer.
    /// </summary>
    ReadOnlyMemory<byte> Body { get; }

    /// <summary>
    /// Gets or sets the transport-level message envelope from which this context was populated.
    /// </summary>
    MessageEnvelope? Envelope { get; set; }

    /// <summary>
    /// Gets or sets the host information describing the application instance that sent this
    /// message.
    /// </summary>
    IRemoteHostInfo Host { get; set; }
}
