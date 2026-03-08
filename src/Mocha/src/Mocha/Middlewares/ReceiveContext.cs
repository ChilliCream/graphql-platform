using Mocha.Features;

namespace Mocha.Middlewares;

/// <summary>
/// Mutable, poolable implementation of <see cref="IReceiveContext"/> and
/// <see cref="IConsumeContext"/> that carries incoming message data through the receive
/// middleware pipeline and consumer invocations.
/// </summary>
/// <remarks>
/// Instances are designed to be reused via object pooling. Call <see cref="Initialize"/> to
/// bind the context to a service scope, endpoint, and runtime, then call
/// <see cref="SetEnvelope"/> to populate message properties from a deserialized envelope.
/// Call <see cref="Reset"/> to return the instance to a clean state before pooling.
/// </remarks>
public sealed class ReceiveContext : IReceiveContext, IConsumeContext
{
    private readonly PooledFeatureCollection _features;
    private readonly Headers _headers = new();

    /// <summary>
    /// Creates a new instance of the <see cref="ReceiveContext"/> with pooled internal storage.
    /// </summary>
    public ReceiveContext()
    {
        _features = new(this);
    }

    /// <summary>
    /// Gets the mutable header collection for the incoming message.
    /// </summary>
    public IHeaders Headers => _headers;

    /// <summary>
    /// Gets the feature collection for storing extensibility data during receive processing.
    /// </summary>
    public IFeatureCollection Features => _features;

    /// <summary>
    /// Gets or sets the scoped service provider for the current receive operation.
    /// </summary>
    public IServiceProvider Services { get; set; } = null!;

    /// <summary>
    /// Gets or sets the messaging runtime that this receive operation executes within.
    /// </summary>
    public IMessagingRuntime Runtime { get; set; } = null!;

    /// <summary>
    /// Gets or sets the transport from which the message was received.
    /// </summary>
    public MessagingTransport Transport { get; set; } = null!;

    /// <summary>
    /// Gets or sets the receive endpoint that owns this receive operation.
    /// </summary>
    public ReceiveEndpoint Endpoint { get; set; } = null!;

    /// <summary>
    /// Gets or sets the deserialized message envelope containing the raw transport payload.
    /// </summary>
    /// <remarks>
    /// Populated by calling <see cref="SetEnvelope"/>. Individual context properties
    /// (message ID, headers, body, etc.) are extracted from this envelope.
    /// </remarks>
    public MessageEnvelope? Envelope { get; set; }

    /// <summary>
    /// Gets or sets the content type of the received message body.
    /// </summary>
    public MessageContentType? ContentType { get; set; }

    /// <summary>
    /// Gets or sets the logical message type descriptor for the received message.
    /// </summary>
    public MessageType? MessageType { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the received message.
    /// </summary>
    public string? MessageId { get; set; }

    /// <summary>
    /// Gets or sets the correlation identifier used to group related messages.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the conversation identifier that links all messages in a logical conversation.
    /// </summary>
    public string? ConversationId { get; set; }

    /// <summary>
    /// Gets or sets the causation identifier referencing the message that caused this one.
    /// </summary>
    public string? CausationId { get; set; }

    /// <summary>
    /// Gets or sets the address of the endpoint that originally sent the message.
    /// </summary>
    public Uri? SourceAddress { get; set; }

    /// <summary>
    /// Gets or sets the address of the endpoint to which the message was delivered.
    /// </summary>
    public Uri? DestinationAddress { get; set; }

    /// <summary>
    /// Gets or sets the address to which responses to this message should be sent.
    /// </summary>
    public Uri? ResponseAddress { get; set; }

    /// <summary>
    /// Gets or sets the address to which fault notifications should be sent.
    /// </summary>
    public Uri? FaultAddress { get; set; }

    /// <summary>
    /// Gets or sets the timestamp indicating when the message was originally sent.
    /// </summary>
    public DateTimeOffset? SentAt { get; set; }

    /// <summary>
    /// Gets or sets an optional deadline by which the message must be delivered.
    /// </summary>
    public DateTimeOffset? DeliverBy { get; set; }

    /// <summary>
    /// Gets or sets the number of times this message has been delivered, including the current attempt.
    /// </summary>
    /// <remarks>
    /// Useful for implementing retry budgets or dead-letter policies in middleware.
    /// </remarks>
    public int? DeliveryCount { get; set; }

    /// <summary>
    /// Gets or sets the raw serialized body of the received message.
    /// </summary>
    public ReadOnlyMemory<byte> Body { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets information about the remote host that sent the message.
    /// </summary>
    public IRemoteHostInfo Host { get; set; } = null!;

    /// <summary>
    /// Gets or sets the cancellation token for the current receive operation.
    /// </summary>
    public CancellationToken CancellationToken { get; set; }

    /// <summary>
    /// Resets all properties, headers, features, and the body to their default state
    /// so the instance can be returned to the object pool.
    /// </summary>
    public void Reset()
    {
        Services = null!;
        Runtime = null!;
        Transport = null!;
        Endpoint = null!;
        Envelope = null!;
        ContentType = null!;
        MessageType = null!;
        MessageId = null!;
        CorrelationId = null!;
        ConversationId = null!;
        CausationId = null!;
        SourceAddress = null!;
        DestinationAddress = null!;
        ResponseAddress = null!;
        FaultAddress = null!;
        SentAt = DateTimeOffset.UtcNow;
        DeliverBy = null;
        DeliveryCount = null;
        Body = Array.Empty<byte>();
        Host = null!;
        CancellationToken = CancellationToken.None;
        _headers.Clear();
        _features.Reset();
    }

    /// <summary>
    /// Populates the context properties from a deserialized message envelope.
    /// </summary>
    /// <remarks>
    /// Extracts message ID, correlation ID, conversation ID, causation ID, addresses,
    /// content type, timestamps, delivery count, body, and headers from the envelope
    /// and applies them to this context. Envelope headers are merged into the existing
    /// header collection, overwriting any keys that collide.
    /// </remarks>
    /// <param name="envelope">The deserialized envelope received from the transport.</param>
    public void SetEnvelope(MessageEnvelope envelope)
    {
        Envelope = envelope;
        MessageId = envelope.MessageId;
        CorrelationId = envelope.CorrelationId;
        ConversationId = envelope.ConversationId;
        CausationId = envelope.CausationId;
        SourceAddress = envelope.SourceAddress.ToUri();
        DestinationAddress = envelope.DestinationAddress.ToUri();
        ResponseAddress = envelope.ResponseAddress.ToUri();
        FaultAddress = envelope.FaultAddress.ToUri();
        ContentType = MessageContentType.Parse(envelope.ContentType);
        SentAt = envelope.SentAt;
        DeliverBy = envelope.DeliverBy;
        DeliveryCount = envelope.DeliveryCount;
        Body = envelope.Body;

        if (envelope.Headers is not null)
        {
            Headers.AddRange(Headers);

            foreach (var header in envelope.Headers)
            {
                Headers.Set(header.Key, header.Value);
            }
        }
    }

    /// <summary>
    /// Prepares this context for a new receive operation by binding it to the specified
    /// endpoint, runtime, and service scope.
    /// </summary>
    /// <remarks>
    /// Sets the <see cref="Services"/>, <see cref="Endpoint"/>, <see cref="Transport"/>,
    /// and <see cref="Runtime"/> properties and initializes the internal feature collection.
    /// </remarks>
    /// <param name="services">The scoped service provider for this receive operation.</param>
    /// <param name="endpoint">The receive endpoint that owns this operation.</param>
    /// <param name="runtime">The messaging runtime providing host info and global options.</param>
    /// <param name="cancellationToken">Reserved for future use; not currently consumed by this method.</param>
    public void Initialize(
        IServiceProvider services,
        ReceiveEndpoint endpoint,
        IMessagingRuntime runtime,
        CancellationToken cancellationToken)
    {
        Services = services;
        Endpoint = endpoint;
        Transport = endpoint.Transport;
        Runtime = runtime;

        _features.Initialize();
    }
}

file static class Extensions
{
    public static Uri? ToUri(this string? address)
    {
        if (address is null)
        {
            return null;
        }

        if (Uri.TryCreate(address, UriKind.Absolute, out var uri))
        {
            return uri;
        }

        return null;
    }
}
