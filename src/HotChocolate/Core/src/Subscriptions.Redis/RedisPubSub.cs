using System.Collections.Concurrent;
using HotChocolate.Execution;
using StackExchange.Redis;
using static System.StringComparer;

namespace HotChocolate.Subscriptions.Redis;

internal sealed class RedisPubSub : ITopicEventReceiver, ITopicEventSender
{
    private readonly ConcurrentDictionary<string, IDisposable> _topics = new(Ordinal);
    private readonly TopicFormatter _formatter;
    private readonly SubscriptionOptions _options;
    private readonly ISubscriptionDiagnosticEvents _diagnosticEvents;
    private readonly ISubscriber _subscriber;
    private readonly IMessageSerializer _serializer;
    private readonly string _completed;

    public RedisPubSub(
        ISubscriber subscriber,
        IMessageSerializer serializer,
        SubscriptionOptions options,
        ISubscriptionDiagnosticEvents diagnosticEvents)
    {
        _subscriber = subscriber ??
            throw new ArgumentNullException(nameof(subscriber));
        _serializer = serializer ??
            throw new ArgumentNullException(nameof(serializer));
        _completed = serializer.CompleteMessage;
        _options = options ??
            throw new ArgumentNullException(nameof(options));
        _formatter = new TopicFormatter(options.TopicPrefix);
        _diagnosticEvents = diagnosticEvents ??
            throw new ArgumentNullException(nameof(diagnosticEvents));
    }

    public async ValueTask<ISourceStream<TMessage>> SubscribeAsync<TMessage>(
        string topic,
        int? bufferCapacity = null,
        TopicBufferFullMode? bufferFullMode = null,
        CancellationToken cancellationToken = default)
    {
        if (topic is null)
        {
            throw new ArgumentNullException(nameof(topic));
        }

        var formattedTopic = _formatter.Format(topic);
        ISourceStream<TMessage>? sourceStream = null;

        while (sourceStream is null)
        {
            var eventTopic = _topics.GetOrAdd(
                formattedTopic,
                t => CreateTopic<TMessage>(t, bufferCapacity, bufferFullMode));

            if (eventTopic is RedisTopic<TMessage> et)
            {
                sourceStream = await et.TrySubscribeAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // we found a topic with the same name but a different message type.
                // this is an invalid state and we will except.
                throw new InvalidMessageTypeException();
            }
        }

        return sourceStream;
    }

    public async ValueTask SendAsync<TMessage>(
        string topic,
        TMessage message,
        CancellationToken cancellationToken = default)
    {
        if (topic is null)
        {
            throw new ArgumentNullException(nameof(topic));
        }

        var formattedTopic = _formatter.Format(topic);
        var envelope = new RedisMessageEnvelope<TMessage>(message, false);
        var serialized = _serializer.Serialize(envelope);
        await _subscriber.PublishAsync(formattedTopic, serialized).ConfigureAwait(false);
    }

    public async ValueTask CompleteAsync(string topic)
    {
        if (topic is null)
        {
            throw new ArgumentNullException(nameof(topic));
        }

        var formattedTopic = _formatter.Format(topic);
        await _subscriber.PublishAsync(formattedTopic, _completed).ConfigureAwait(false);
    }

    private RedisTopic<TMessage> CreateTopic<TMessage>(
        string topic,
        int? bufferCapacity,
        TopicBufferFullMode? bufferFullMode)
    {
        var eventTopic = new RedisTopic<TMessage>(
            topic,
            _subscriber,
            _serializer,
            bufferCapacity ?? _options.TopicBufferCapacity,
            bufferFullMode ?? _options.TopicBufferFullMode,
            _diagnosticEvents);

        eventTopic.Unsubscribed += (sender, __) =>
        {
            var s = (RedisTopic<TMessage>)sender!;
            _topics.TryRemove(s.Name, out _);
            s.Dispose();
        };

        return eventTopic;
    }
}
