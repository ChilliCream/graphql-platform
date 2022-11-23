using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using static System.StringComparer;

namespace HotChocolate.Subscriptions.InMemory;

public class InMemoryPubSub : ITopicEventReceiver, ITopicEventSender
{
    private readonly ConcurrentDictionary<string, IEventTopic> _topics = new(Ordinal);

    public ValueTask SendAsync<TMessage>(
        string topic,
        TMessage message,
        CancellationToken cancellationToken = default)
    {
        var eventTopic = _topics.GetOrAdd(topic, t => CreateTopic<TMessage>(t));

        if (eventTopic is EventTopic<TMessage> et)
        {
            et.TryWrite(message);
            return default;
        }

        throw new InvalidMessageTypeException();
    }

    public async ValueTask CompleteAsync(string topic)
    {
        if (_topics.TryRemove(topic, out var eventTopic))
        {
            await eventTopic.CompleteAsync().ConfigureAwait(false);
        }
    }

    public async ValueTask<ISourceStream<TMessage>> SubscribeAsync<TMessage>(
        string topic,
        CancellationToken cancellationToken = default)
    {
        var eventTopic = _topics.GetOrAdd(topic, t => CreateTopic<TMessage>(t));

        if (eventTopic is EventTopic<TMessage> et)
        {
            return await et.SubscribeAsync(cancellationToken).ConfigureAwait(false);
        }

        throw new InvalidMessageTypeException();
    }

    private EventTopic<TMessage> CreateTopic<TMessage>(string topic)
    {
        var eventTopic = new EventTopic<TMessage>(topic);
        eventTopic.Unsubscribed += (sender, __) =>
        {
            var s = (EventTopic<TMessage>)sender!;
            _topics.TryRemove(s.Topic, out _);
            s.Dispose();
        };
        return eventTopic;
    }
}
