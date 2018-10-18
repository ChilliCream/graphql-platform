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
                        stream.Trigger();
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<IEventStream> SubscribeAsync(
            IEventDescription eventReference)
        {
            if (eventReference == null)
            {
                throw new ArgumentNullException(nameof(eventReference));
            }

            await _semaphore.WaitAsync();

            try
            {
                if (!_streams.TryGetValue(eventReference, out var subscribers))
                {
                    subscribers = new List<InMemoryEventStream>();
                    _streams[eventReference] = subscribers;
                }

                var eventMessage = new EventMessage(eventReference);
                var stream = new InMemoryEventStream(eventMessage);
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

