using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;

namespace HotChocolate.Subscriptions.InMemory
{
    public class InMemoryPubSub
        : ITopicEventReceiver
        , ITopicEventSender
    {
        private readonly ConcurrentDictionary<object, IEventTopic> _topics =
            new ConcurrentDictionary<object, IEventTopic>();

        public ValueTask SendAsync<TTopic, TMessage>(
            TTopic topic,
            TMessage message,
            CancellationToken cancellationToken = default)
            where TTopic : notnull
        {
            IEventTopic eventTopic = _topics.GetOrAdd(topic, s => new EventTopic<TMessage>());

            if (eventTopic is EventTopic<TMessage> et)
            {
                et.TryWrite(message);
                return default;
            }

            throw new InvalidMessageTypeException();
        }

        public async ValueTask CompleteAsync<TTopic>(TTopic topic)
            where TTopic : notnull
        {
            if (_topics.TryRemove(topic, out IEventTopic? eventTopic))
            {
                await eventTopic.CompleteAsync().ConfigureAwait(false);
            }
        }

        public async ValueTask<ISourceStream<TMessage>> SubscribeAsync<TTopic, TMessage>(
            TTopic topic,
            CancellationToken cancellationToken = default)
            where TTopic : notnull
        {
            IEventTopic eventTopic = _topics.GetOrAdd(topic, s => new EventTopic<TMessage>());

            if (eventTopic is EventTopic<TMessage> et)
            {
                return await et.SubscribeAsync(cancellationToken).ConfigureAwait(false);
            }

            throw new InvalidMessageTypeException();
        }
    }
}
