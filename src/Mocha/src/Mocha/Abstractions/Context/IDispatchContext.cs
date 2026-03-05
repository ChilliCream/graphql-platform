using System.Collections.Immutable;
using Mocha.Utils;

namespace Mocha.Middlewares;

/// <summary>
/// Represents the context for an outbound message dispatch operation, carrying all metadata and
/// payload through the dispatch middleware pipeline.
/// </summary>
public interface IDispatchContext : IExecutionContext, IFeatureProvider
{
    /// <summary>
    /// Gets or sets the transport over which the message will be dispatched.
    /// </summary>
    MessagingTransport Transport { get; set; }

    /// <summary>
    /// Gets or sets the dispatch endpoint that will handle sending the message.
    /// </summary>
    DispatchEndpoint Endpoint { get; set; }

    /// <summary>
    /// Gets or sets the content type used to serialize the message body.
    /// </summary>
    MessageContentType? ContentType { get; set; }

    /// <summary>
    /// Gets or sets the message type descriptor for the outbound message.
    /// </summary>
    MessageType? MessageType { get; set; }

    /// <summary>
    /// Gets or sets the message payload object before serialization.
    /// </summary>
    object? Message { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier for this message.
    /// </summary>
    string? MessageId { get; set; }

    /// <summary>
    /// Gets or sets the correlation identifier used to group related messages in a workflow.
    /// </summary>
    string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the conversation identifier that tracks a logical conversation across multiple messages.
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
    /// Gets or sets the address of the target endpoint.
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
    /// Gets the mutable header collection for the outbound message.
    /// </summary>
    IHeaders Headers { get; }

    /// <summary>
    /// Gets or sets the timestamp indicating when the message was sent.
    /// </summary>
    DateTimeOffset SentAt { get; set; }

    /// <summary>
    /// Gets or sets the optional deadline by which the message must be delivered before it is considered expired.
    /// </summary>
    DateTimeOffset? DeliverBy { get; set; }

    /// <summary>
    /// Gets the writable memory buffer used to hold the serialized message body.
    /// </summary>
    IWritableMemory Body { get; }

    /// <summary>
    /// Gets or sets the host information describing the sending application instance.
    /// </summary>
    IRemoteHostInfo Host { get; set; }

    /// <summary>
    /// Gets or sets the transport-level message envelope, populated after serialization.
    /// </summary>
    MessageEnvelope? Envelope { get; set; }
}
