using Mocha.Middlewares;

namespace Mocha;

internal sealed class ConsumeContext<TMessage> : IConsumeContext<TMessage>, IDisposable
{
    private IConsumeContext? _inner;

    public ConsumeContext(IConsumeContext inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    private IConsumeContext Inner => _inner ?? throw new ObjectDisposedException(nameof(ConsumeContext<TMessage>));

    public TMessage Message
        => field ??=
            Inner.GetMessage<TMessage>() ?? throw new InvalidOperationException("Could not deserialize message");

    public IFeatureCollection Features => Inner.Features;

    public IReadOnlyHeaders Headers => Inner.Headers;

    public MessagingTransport Transport
    {
        get => Inner.Transport;
        set => Inner.Transport = value;
    }

    public ReceiveEndpoint Endpoint
    {
        get => Inner.Endpoint;
        set => Inner.Endpoint = value;
    }

    public string? MessageId
    {
        get => Inner.MessageId;
        set => Inner.MessageId = value;
    }

    public string? CorrelationId
    {
        get => Inner.CorrelationId;
        set => Inner.CorrelationId = value;
    }

    public string? ConversationId
    {
        get => Inner.ConversationId;
        set => Inner.ConversationId = value;
    }

    public string? CausationId
    {
        get => Inner.CausationId;
        set => Inner.CausationId = value;
    }

    public Uri? SourceAddress
    {
        get => Inner.SourceAddress;
        set => Inner.SourceAddress = value;
    }

    public Uri? DestinationAddress
    {
        get => Inner.DestinationAddress;
        set => Inner.DestinationAddress = value;
    }

    public Uri? ResponseAddress
    {
        get => Inner.ResponseAddress;
        set => Inner.ResponseAddress = value;
    }

    public Uri? FaultAddress
    {
        get => Inner.FaultAddress;
        set => Inner.FaultAddress = value;
    }

    public MessageContentType? ContentType
    {
        get => Inner.ContentType;
        set => Inner.ContentType = value;
    }

    public MessageType? MessageType
    {
        get => Inner.MessageType;
        set => Inner.MessageType = value;
    }

    public DateTimeOffset? SentAt
    {
        get => Inner.SentAt;
        set => Inner.SentAt = value;
    }

    public DateTimeOffset? DeliverBy
    {
        get => Inner.DeliverBy;
        set => Inner.DeliverBy = value;
    }

    public int? DeliveryCount
    {
        get => Inner.DeliveryCount;
        set => Inner.DeliveryCount = value;
    }

    public ReadOnlyMemory<byte> Body => Inner.Body;

    public MessageEnvelope? Envelope
    {
        get => Inner.Envelope;
        set => Inner.Envelope = value;
    }

    public IRemoteHostInfo Host
    {
        get => Inner.Host;
        set => Inner.Host = value;
    }

    public IMessagingRuntime Runtime
    {
        get => Inner.Runtime;
        set => Inner.Runtime = value;
    }

    public CancellationToken CancellationToken
    {
        get => Inner.CancellationToken;
        set => Inner.CancellationToken = value;
    }

    public IServiceProvider Services
    {
        get => Inner.Services;
        set => Inner.Services = value;
    }

    public void Dispose()
    {
        _inner = null;
    }
}
