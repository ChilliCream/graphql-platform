using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Subscriptions
{
    public class InMemoryEventRegistry
        : IEventRegistry
        , IEventSender
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private readonly Dictionary<Event, List<InMemoryEventStream>> _streams =
            new Dictionary<Event, List<InMemoryEventStream>>();

        public async Task SendAsync(Event @event)
        {
            await _semaphore.WaitAsync();

            try
            {
                if (_streams.TryGetValue(@event, out var subscribers))
                {
                    foreach (var stream in subscribers)
                    {
                        stream.Trigger();
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<IEventStream> SubscribeAsync(Event @event)
        {
            await _semaphore.WaitAsync();

            try
            {
                if (!_streams.TryGetValue(@event, out var subscribers))
                {
                    subscribers = new List<InMemoryEventStream>();
                    _streams[@event] = subscribers;
                }

                var stream = new InMemoryEventStream();
                stream.Disposed += (s, e) => Unsubscribe(@event, stream);
                subscribers.Add(stream);
                return stream;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private void Unsubscribe(Event @event, InMemoryEventStream stream)
        {
            _semaphore.Wait();

            try
            {
                if (_streams.TryGetValue(@event, out var subscribers))
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

