using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Subscriptions
{
    [Obsolete]
    public class InMemoryEventRegistry
        : IEventRegistry
        , IEventSender
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private readonly Dictionary<IEventDescription, List<InMemoryEventStream>> _streams =
            new Dictionary<IEventDescription, List<InMemoryEventStream>>();

        public async ValueTask SendAsync(
            IEventMessage message,
            CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                if (_streams.TryGetValue(message.Event,
                    out List<InMemoryEventStream> subscribers))
                {
                    foreach (InMemoryEventStream stream in subscribers)
                    {
                        await stream.TriggerAsync(message, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public ValueTask<IEventStream> SubscribeAsync(
            IEventDescription eventDescription,
            CancellationToken cancellationToken = default)
        {
            if (eventDescription is null)
            {
                throw new ArgumentNullException(nameof(eventDescription));
            }

            return SubscribeInternalAsync(eventDescription);
        }

        private async ValueTask<IEventStream> SubscribeInternalAsync(
            IEventDescription eventDescription)
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                if (!_streams.TryGetValue(eventDescription,
                    out List<InMemoryEventStream> subscribers))
                {
                    subscribers = new List<InMemoryEventStream>();
                    _streams[eventDescription] = subscribers;
                }

                var eventMessage = new EventMessage(eventDescription);
                var stream = new InMemoryEventStream();
                stream.Completed += (s, e) => Unsubscribe(eventMessage, stream);
                subscribers.Add(stream);
                return stream;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private void Unsubscribe(
            IEventMessage message,
            InMemoryEventStream stream)
        {
            _semaphore.Wait();

            try
            {
                if (_streams.TryGetValue(message.Event,
                    out List<InMemoryEventStream> subscribers))
                {
                    subscribers.Remove(stream);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
