using Mocha.Features;
using Mocha.Utils;

namespace Mocha.Middlewares;

/// <summary>
/// Mutable, poolable implementation of <see cref="IDispatchContext"/> that carries outgoing
/// message data through the dispatch middleware pipeline.
/// </summary>
/// <remarks>
/// Instances are designed to be reused via object pooling. Call <see cref="Initialize"/> to
/// prepare the context for a new dispatch, and call <see cref="Reset"/> to return it to a
/// clean state before returning it to the pool. The internal feature collection, headers,
/// and body writer are all pool-aware and cleared on reset.
/// </remarks>
public sealed class DispatchContext : IDispatchContext
{
    private readonly PooledFeatureCollection _features;
    private readonly PooledArrayWriter _writer = new();
    private readonly Headers _headers = new();

    /// <summary>
    /// Creates a new instance of the <see cref="DispatchContext"/> with pooled internal storage.
    /// </summary>
    public DispatchContext()
    {
        _features = new(this);
    }

    /// <summary>
    /// Gets the mutable header collection for the outgoing message.
    /// </summary>
    public IHeaders Headers => _headers;

    /// <summary>
    /// Gets the feature collection for storing extensibility data during dispatch.
    /// </summary>
    public IFeatureCollection Features => _features;

    /// <summary>
    /// Gets or sets the scoped service provider for the current dispatch operation.
    /// </summary>
    public IServiceProvider Services { get; set; } = null!;

    /// <summary>
    /// Gets or sets the messaging runtime that this dispatch executes within.
    /// </summary>
    public IMessagingRuntime Runtime { get; set; } = null!;

    /// <summary>
    /// Gets or sets the transport that the message will be dispatched through.
    /// </summary>
    public MessagingTransport Transport { get; set; } = null!;

    /// <summary>
    /// Gets or sets the dispatch endpoint that owns this dispatch operation.
    /// </summary>
    public DispatchEndpoint Endpoint { get; set; } = null!;

    /// <summary>
    /// Gets or sets the cancellation token for the current dispatch operation.
    /// </summary>
    public CancellationToken CancellationToken { get; set; }

    /// <summary>
    /// Gets or sets the content type used to serialize the message body.
    /// </summary>
    /// <remarks>
    /// Defaults to the transport's or runtime's <c>DefaultContentType</c> during
    /// <see cref="Initialize"/> if not explicitly set beforehand.
    /// </remarks>
    public MessageContentType? ContentType { get; set; }

    /// <summary>
    /// Gets or sets the logical message type descriptor for the outgoing message.
    /// </summary>
    public MessageType? MessageType { get; set; }

    /// <summary>
    /// Gets or sets the CLR message object to be serialized and dispatched.
    /// </summary>
    public object? Message { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the outgoing message.
    /// </summary>
    /// <remarks>
    /// Auto-generated as a version-7 GUID during <see cref="Initialize"/> if not set.
    /// </remarks>
    public string? MessageId { get; set; }

    /// <summary>
    /// Gets or sets the correlation identifier used to group related messages.
    /// </summary>
    /// <remarks>
    /// Auto-generated as a version-7 GUID during <see cref="Initialize"/> if not set.
    /// </remarks>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the conversation identifier that links all messages in a logical conversation.
    /// </summary>
    /// <remarks>
    /// Auto-generated as a version-7 GUID during <see cref="Initialize"/> if not set.
    /// </remarks>
    public string? ConversationId { get; set; }

    /// <summary>
    /// Gets or sets the causation identifier referencing the message that caused this dispatch.
    /// </summary>
    public string? CausationId { get; set; }

    /// <summary>
    /// Gets or sets the address of the endpoint that originated this message.
    /// </summary>
    /// <remarks>
    /// Defaults to the transport's reply receive endpoint source address during
    /// <see cref="Initialize"/> if not explicitly set.
    /// </remarks>
    public Uri? SourceAddress { get; set; }

    /// <summary>
    /// Gets or sets the address of the endpoint to which this message is being sent.
    /// </summary>
    /// <remarks>
    /// Defaults to the dispatch endpoint's address during <see cref="Initialize"/>
    /// if not explicitly set.
    /// </remarks>
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
    /// Gets or sets the timestamp indicating when the message was sent.
    /// </summary>
    /// <remarks>
    /// Refreshed to <see cref="DateTimeOffset.UtcNow"/> during <see cref="Initialize"/> and <see cref="Reset"/>.
    /// </remarks>
    public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets an optional deadline by which the message must be delivered.
    /// </summary>
    public DateTimeOffset? DeliverBy { get; set; }

    /// <summary>
    /// Gets or sets the optional time at which the message should be made available for consumption.
    /// </summary>
    public DateTimeOffset? ScheduledTime { get; set; }

    /// <summary>
    /// Gets or sets the serialized message envelope, available after serialization middleware runs.
    /// </summary>
    public MessageEnvelope? Envelope { get; set; }

    /// <summary>
    /// Gets or sets information about the host that is sending the message.
    /// </summary>
    public IRemoteHostInfo Host { get; set; } = null!;

    /// <summary>
    /// Gets the writable buffer that receives the serialized message body bytes.
    /// </summary>
    public IWritableMemory Body => _writer;

    /// <summary>
    /// Resets all properties, headers, features, and the body writer to their default state
    /// so the instance can be returned to the object pool.
    /// </summary>
    public void Reset()
    {
        Services = null!;
        Runtime = null!;
        Transport = null!;
        Endpoint = null!;
        CancellationToken = CancellationToken.None;
        ContentType = null!;
        MessageType = null!;
        Message = null!;
        MessageId = null!;
        CorrelationId = null;
        ConversationId = null!;
        CausationId = null!;
        SourceAddress = null;
        DestinationAddress = null;
        ResponseAddress = null!;
        FaultAddress = null!;
        SentAt = DateTimeOffset.UtcNow;
        DeliverBy = null;
        ScheduledTime = null;
        Host = null!;
        Envelope = null!;
        _headers.Clear();
        _features.Reset();
        _writer.Reset();
    }

    /// <summary>
    /// Prepares this context for a new dispatch operation by binding it to the specified
    /// endpoint, runtime, and service scope.
    /// </summary>
    /// <remarks>
    /// Sets default values for <see cref="ContentType"/>, <see cref="DestinationAddress"/>,
    /// <see cref="SourceAddress"/>, <see cref="MessageId"/>, <see cref="CorrelationId"/>,
    /// and <see cref="ConversationId"/> if they have not been explicitly set. Initializes
    /// the internal feature collection.
    /// </remarks>
    /// <param name="services">The scoped service provider for this dispatch operation.</param>
    /// <param name="endpoint">The dispatch endpoint that owns this operation.</param>
    /// <param name="runtime">The messaging runtime providing host info and global options.</param>
    /// <param name="messageType">The logical message type descriptor, or <see langword="null"/> if untyped.</param>
    /// <param name="cancellationToken">Token to signal cancellation of the dispatch.</param>
    public void Initialize(
        IServiceProvider services,
        DispatchEndpoint endpoint,
        IMessagingRuntime runtime,
        MessageType? messageType,
        CancellationToken cancellationToken)
    {
        Services = services;
        Endpoint = endpoint;
        Transport = endpoint.Transport;
        Host = runtime.Host;
        CancellationToken = cancellationToken;
        SentAt = DateTimeOffset.UtcNow;
        MessageType = messageType;
        ContentType ??= endpoint.Transport.Options.DefaultContentType ?? runtime.Options.DefaultContentType;
        DestinationAddress ??= endpoint.Address;
        SourceAddress ??= endpoint.Transport.ReplyReceiveEndpoint?.Source.Address;

        MessageId ??= Guid.NewGuid().ToString();
        CorrelationId ??= Guid.NewGuid().ToString();
        ConversationId ??= Guid.NewGuid().ToString();

        _features.Initialize();
    }
}
