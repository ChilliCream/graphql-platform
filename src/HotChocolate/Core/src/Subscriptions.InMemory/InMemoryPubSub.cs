using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using static System.StringComparer;

namespace HotChocolate.Subscriptions.InMemory;

public class InMemoryPubSub : ITopicEventReceiver, ITopicEventSender
{
    private readonly ConcurrentDictionary<string, IInMemoryTopic> _topics = new(Ordinal);
    private readonly SubscriptionOptions _options;
    private readonly ISubscriptionDiagnosticEvents _diagnosticEvents;

    public InMemoryPubSub(
        SubscriptionOptions options,
        ISubscriptionDiagnosticEvents diagnosticEvents)
    {
        _options = options;
        _diagnosticEvents = diagnosticEvents;
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

        ISourceStream<TMessage>? sourceStream = null;

        while (sourceStream is null)
        {
            var eventTopic = _topics.GetOrAdd(
                topic,
                t => CreateTopic<TMessage>(t, bufferCapacity, bufferFullMode));

            if (eventTopic is InMemoryTopic<TMessage> et)
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

    public ValueTask SendAsync<TMessage>(
        string topic,
        TMessage message,
        CancellationToken cancellationToken = default)
    {
        if (topic is null)
        {
            throw new ArgumentNullException(nameof(topic));
        }

        if (_topics.TryGetValue(topic, out var eventTopic))
        {
            if (eventTopic is InMemoryTopic<TMessage> et)
            {
                et.TryWrite(message);
            }
            else
            {
                throw new InvalidMessageTypeException();
            }
        }

        return default;
    }

    public ValueTask CompleteAsync(string topic)
    {
        if (topic is null)
        {
            throw new ArgumentNullException(nameof(topic));
        }

        if (_topics.TryGetValue(topic, out var eventTopic))
        {
            eventTopic.TryComplete();
        }

        return default;
    }

    private InMemoryTopic<TMessage> CreateTopic<TMessage>(
        string topic,
        int? bufferCapacity,
        TopicBufferFullMode? bufferFullMode)
    {
        var eventTopic = new InMemoryTopic<TMessage>(
            topic,
            bufferCapacity ?? _options.TopicBufferCapacity,
            bufferFullMode ?? _options.TopicBufferFullMode,
            _diagnosticEvents);

        eventTopic.Unsubscribed += (sender, __) =>
        {
            var s = (InMemoryTopic<TMessage>)sender!;
            _topics.TryRemove(s.Name, out _);
            s.Dispose();
        };

        return eventTopic;
    }
}
