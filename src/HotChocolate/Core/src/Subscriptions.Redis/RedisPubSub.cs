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
    private readonly string? _topicPrefix;

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
        _topicPrefix = options.TopicPrefix;
    }

    protected override async ValueTask OnSendAsync<TMessage>(
        string formattedTopic,
        TMessage message,
        CancellationToken cancellationToken = default)
    {
        var serialized = _serializer.Serialize(message);

        // The object returned from GetSubscriber is a cheap pass-thru object that does not need
        // to be stored.
        var subscriber = _connection.GetSubscriber();
        await subscriber.PublishAsync(GetPrefixedTopic(formattedTopic), serialized).ConfigureAwait(false);
    }

    protected override async ValueTask OnCompleteAsync(string formattedTopic)
    {
        // The object returned from GetSubscriber is a cheap pass-thru object that does not need
        // to be stored.
        var subscriber = _connection.GetSubscriber();
        await subscriber.PublishAsync(GetPrefixedTopic(formattedTopic), _completed).ConfigureAwait(false);
    }

    protected override DefaultTopic<TMessage> OnCreateTopic<TMessage>(
        string formattedTopic,
        int? bufferCapacity,
        TopicBufferFullMode? bufferFullMode)
        => new RedisTopic<TMessage>(
            GetPrefixedTopic(formattedTopic),
            _connection,
            _serializer,
            bufferCapacity ?? _topicBufferCapacity,
            bufferFullMode ?? _topicBufferFullMode,
            DiagnosticEvents);

    private string GetPrefixedTopic(string topic)
        => string.IsNullOrWhiteSpace(_topicPrefix)
            ? topic
            : _topicPrefix + topic;
}
