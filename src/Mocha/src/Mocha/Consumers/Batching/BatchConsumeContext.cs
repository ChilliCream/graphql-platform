using Mocha.Features;
using Mocha.Middlewares;

namespace Mocha;

internal sealed class BatchConsumeContext<TMessage> : IBatchConsumeContext<TMessage>
{
    private readonly Headers _headers = new(0);
    private readonly FeatureCollection _features = new();

    public BatchConsumeContext(
        MessageBatch<TMessage> message,
        IServiceProvider services,
        IConsumeContext firstContext,
        string batchId,
        MessageType? itemMessageType,
        CancellationToken cancellationToken)
    {
        Message = message;
        Services = services;
        Runtime = firstContext.Runtime;
        Transport = firstContext.Transport;
        Endpoint = firstContext.Endpoint;
        Host = firstContext.Host;
        BatchId = batchId;
        ItemMessageType = itemMessageType;
        CancellationToken = cancellationToken;
    }

    public IMessageBatch<TMessage> Message { get; }

    public string BatchId { get; }

    public MessageType? ItemMessageType { get; }

    public IFeatureCollection Features => _features;

    public MessagingTransport Transport { get; set; }

    public ReceiveEndpoint Endpoint { get; set; }

    public string? MessageId { get; set; }

    public string? CorrelationId { get; set; }

    public string? ConversationId { get; set; }

    public string? CausationId { get; set; }

    public Uri? SourceAddress { get; set; }

    public Uri? DestinationAddress { get; set; }

    public Uri? ResponseAddress { get; set; }

    public Uri? FaultAddress { get; set; }

    public MessageContentType? ContentType { get; set; }

    public MessageType? MessageType { get; set; }

    public IReadOnlyHeaders Headers => _headers;

    public DateTimeOffset? SentAt { get; set; }

    public DateTimeOffset? DeliverBy { get; set; }

    public int? DeliveryCount { get; set; }

    public ReadOnlyMemory<byte> Body => ReadOnlyMemory<byte>.Empty;

    public MessageEnvelope? Envelope { get; set; }

    public IRemoteHostInfo Host { get; set; }

    public IMessagingRuntime Runtime { get; set; }

    public CancellationToken CancellationToken { get; set; }

    public IServiceProvider Services { get; set; }
}
