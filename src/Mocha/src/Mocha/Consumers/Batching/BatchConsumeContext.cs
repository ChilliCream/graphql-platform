using Mocha.Features;
using Mocha.Middlewares;

namespace Mocha;

internal sealed class BatchConsumeContext<TMessage> : IBatchConsumeContext<TMessage>
{
    private readonly Headers _headers;
    private readonly FeatureCollection _features;

    public BatchConsumeContext(
        MessageBatch<TMessage> message,
        IServiceProvider services,
        IConsumeContext firstContext,
        string batchId,
        MessageType? itemMessageType,
        CancellationToken cancellationToken)
    {
        _headers = new Headers(0);
        _features = new FeatureCollection();
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

    private BatchConsumeContext(BatchConsumeContext<TMessage> context, IServiceProvider services)
    {
        _headers = new Headers(context._headers);
        _features = new FeatureCollection(context._features);
        Message = context.Message;
        BatchId = context.BatchId;
        ItemMessageType = context.ItemMessageType;
        Services = services;
        Runtime = context.Runtime;
        Transport = context.Transport;
        Endpoint = context.Endpoint;
        Host = context.Host;
        MessageId = context.MessageId;
        CorrelationId = context.CorrelationId;
        ConversationId = context.ConversationId;
        CausationId = context.CausationId;
        SourceAddress = context.SourceAddress;
        DestinationAddress = context.DestinationAddress;
        ResponseAddress = context.ResponseAddress;
        FaultAddress = context.FaultAddress;
        ContentType = context.ContentType;
        MessageType = context.MessageType;
        SentAt = context.SentAt;
        DeliverBy = context.DeliverBy;
        DeliveryCount = context.DeliveryCount;
        Envelope = context.Envelope is { } envelope ? new MessageEnvelope(envelope) : null;
        CancellationToken = context.CancellationToken;
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

    public IConsumeContext Clone(IServiceProvider services)
        => new BatchConsumeContext<TMessage>(this, services);
}
