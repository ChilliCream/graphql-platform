using System;
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
        private readonly Dictionary<IEventDescription, List<InMemoryEventStream>> _streams =
            new Dictionary<IEventDescription, List<InMemoryEventStream>>();

        public async Task SendAsync(IEventMessage message)
        {
            await _semaphore.WaitAsync();

            try
            {
                if (_streams.TryGetValue(message.Event, out var subscribers))
                {
                    foreach (var stream in subscribers)
                    {
                        stream.Trigger(message);
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public Task<IEventStream> SubscribeAsync(
            IEventDescription eventDescription)
        {
            if (eventDescription == null)
            {
                throw new ArgumentNullException(nameof(eventDescription));
            }

            return SubscribeInternalAsync(eventDescription);
        }

        private async Task<IEventStream> SubscribeInternalAsync(
            IEventDescription eventDescription)
        {
            await _semaphore.WaitAsync();

            try
            {
                if (!_streams.TryGetValue(eventDescription,
                    out var subscribers))
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
                if (_streams.TryGetValue(message.Event, out var subscribers))
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

