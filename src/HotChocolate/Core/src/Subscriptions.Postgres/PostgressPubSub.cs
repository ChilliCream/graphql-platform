using HotChocolate.Subscriptions.Diagnostics;

namespace HotChocolate.Subscriptions.Postgres;

internal sealed class PostgresPubSub : DefaultPubSub
{
    private readonly IMessageSerializer _serializer;
    private readonly PostgresChannel _channel;

    private readonly int _maxMessagePayloadSize;
    private readonly int _topicBufferCapacity;
    private readonly TopicBufferFullMode _topicBufferFullMode;

    /// <inheritdoc />
    public PostgresPubSub(
        IMessageSerializer serializer,
        PostgresSubscriptionOptions options,
        ISubscriptionDiagnosticEvents diagnosticEvents,
        PostgresChannel channel)
        : base(options.SubscriptionOptions, diagnosticEvents)
    {
        _serializer = serializer;
        _channel = channel;
        _maxMessagePayloadSize = options.MaxMessagePayloadSize;
        _topicBufferCapacity = options.SubscriptionOptions.TopicBufferCapacity;
        _topicBufferFullMode = options.SubscriptionOptions.TopicBufferFullMode;
    }

    /// <inheritdoc />
    protected override async ValueTask OnSendAsync<TMessage>(
        string formattedTopic,
        TMessage message,
        CancellationToken cancellationToken = default)
    {
        var serialized = _serializer.Serialize(message);

        var envelope = PostgresMessageEnvelope
            .Create(formattedTopic, serialized, _maxMessagePayloadSize);

        await _channel.SendAsync(envelope, cancellationToken);
    }

    /// <inheritdoc />
    protected override async ValueTask OnCompleteAsync(string formattedTopic)
    {
        var envelope = PostgresMessageEnvelope
            .Create(formattedTopic, _serializer.CompleteMessage, _maxMessagePayloadSize);

        await _channel.SendAsync(envelope, cancellationToken: default);
    }

    /// <inheritdoc />
    protected override DefaultTopic<TMessage> OnCreateTopic<TMessage>(
        string formattedTopic,
        int? bufferCapacity,
        TopicBufferFullMode? bufferFullMode)
    {
        return new PostgresTopic<TMessage>(
            formattedTopic,
            bufferCapacity ?? _topicBufferCapacity,
            _serializer,
            _channel,
            bufferFullMode ?? _topicBufferFullMode,
            DiagnosticEvents);
    }
}
