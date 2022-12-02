using AlterNats;
using HotChocolate.Subscriptions.Diagnostics;

namespace HotChocolate.Subscriptions.Nats;

/// <summary>
/// The NATS pub/sub provider implementation.
/// </summary>
internal sealed class NatsPubSub : DefaultPubSub
{
    private readonly NatsConnection _connection;
    private readonly IMessageSerializer _serializer;
    private readonly string _completed;
    private readonly int _topicBufferCapacity;
    private readonly TopicBufferFullMode _topicBufferFullMode;

    public NatsPubSub(
        NatsConnection connection,
        IMessageSerializer serializer,
        SubscriptionOptions options,
        ISubscriptionDiagnosticEvents diagnosticEvents)
        : base(options, diagnosticEvents)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _topicBufferCapacity = options.TopicBufferCapacity;
        _topicBufferFullMode = options.TopicBufferFullMode;
        _completed = serializer.CompleteMessage;
    }

    protected override async ValueTask OnSendAsync<TMessage>(
        string formattedTopic,
        MessageEnvelope<TMessage> message,
        CancellationToken cancellationToken = default)
    {
        var serialized = _serializer.Serialize(message);
        await _connection.PublishAsync(formattedTopic, serialized).ConfigureAwait(false);
    }

    protected override async ValueTask OnCompleteAsync(string formattedTopic)
        => await _connection.PublishAsync(formattedTopic, _completed).ConfigureAwait(false);

    protected override DefaultTopic<TMessage> OnCreateTopic<TMessage>(
        string formattedTopic,
        int? bufferCapacity,
        TopicBufferFullMode? bufferFullMode)
        => new NatsTopic<TMessage>(
            formattedTopic,
            _connection,
            _serializer,
            bufferCapacity ?? _topicBufferCapacity,
            bufferFullMode ?? _topicBufferFullMode,
            DiagnosticEvents);
}
