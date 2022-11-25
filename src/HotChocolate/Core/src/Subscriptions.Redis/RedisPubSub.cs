using HotChocolate.Subscriptions.Diagnostics;
using StackExchange.Redis;

namespace HotChocolate.Subscriptions.Redis;

/// <summary>
/// The redis pub/sub provider implementation.
/// </summary>
internal sealed class RedisPubSub : DefaultPubSub
{
    private readonly IConnectionMultiplexer _connection;
    private readonly IMessageSerializer _serializer;
    private readonly string _completed;
    private readonly int _topicBufferCapacity;
    private readonly TopicBufferFullMode _topicBufferFullMode;
    public RedisPubSub(
        IConnectionMultiplexer connection,
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

        // The object returned from GetSubscriber is a cheap pass-thru object that does not need
        // to be stored.
        var subscriber = _connection.GetSubscriber();
        await subscriber.PublishAsync(formattedTopic, serialized).ConfigureAwait(false);
    }

    protected override async ValueTask OnCompleteAsync(string formattedTopic)
    {
        // The object returned from GetSubscriber is a cheap pass-thru object that does not need
        // to be stored.
        var subscriber = _connection.GetSubscriber();
        await subscriber.PublishAsync(formattedTopic, _completed).ConfigureAwait(false);
    }

    protected override DefaultTopic<TMessage> OnCreateTopic<TMessage>(
        string formattedTopic,
        int? bufferCapacity,
        TopicBufferFullMode? bufferFullMode)
        => new RedisTopic<TMessage>(
            formattedTopic,
            _connection,
            _serializer,
            bufferCapacity ?? _topicBufferCapacity,
            bufferFullMode ?? _topicBufferFullMode,
            DiagnosticEvents);
}
