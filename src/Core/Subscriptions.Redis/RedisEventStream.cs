using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace HotChocolate.Subscriptions.Redis
{
    public class RedisEventStream
        : IEventStream
    {
        private readonly IEventDescription _eventDescription;
        private readonly ChannelMessageQueue _channel;
        private readonly IPayloadSerializer _serializer;
        private RedisEventStreamEnumerator _enumerator;
        private bool _isCompleted;

        public RedisEventStream(
            IEventDescription eventDescription,
            ChannelMessageQueue channel,
            IPayloadSerializer serializer)
        {
            _eventDescription = eventDescription;
            _channel = channel;
            _serializer = serializer;
        }

        public IAsyncEnumerator<IEventMessage> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
        {
            if (_isCompleted || (_enumerator is { IsCompleted: true }))
            {
                throw new InvalidOperationException(
                    "The stream has be completed and cannot be replayed");
            }

            if (_enumerator is null)
            {
                _enumerator = new RedisEventStreamEnumerator(
                    _eventDescription,
                    _channel,
                    _serializer,
                    cancellationToken);
            }

            return _enumerator;
        }

        public async ValueTask CompleteAsync(
            CancellationToken cancellationToken = default)
        {
            if (!_isCompleted)
            {
                await _channel.UnsubscribeAsync().ConfigureAwait(false);
                _isCompleted = true;
            }
        }

        private class RedisEventStreamEnumerator
            : IAsyncEnumerator<IEventMessage>
        {
            private readonly IEventDescription _eventDescription;
            private readonly ChannelMessageQueue _channel;
            private readonly IPayloadSerializer _serializer;
            private readonly CancellationToken _cancellationToken;
            private bool _isCompleted;

            public RedisEventStreamEnumerator(
                IEventDescription eventDescription,
                ChannelMessageQueue channel,
                IPayloadSerializer serializer,
                CancellationToken cancellationToken)
            {
                _eventDescription = eventDescription;
                _channel = channel;
                _serializer = serializer;
                _cancellationToken = cancellationToken;
            }

            public IEventMessage Current { get; private set; }

            internal bool IsCompleted => _isCompleted;

            public async ValueTask<bool> MoveNextAsync()
            {
                if (_isCompleted || _channel.Completion.IsCompleted)
                {
                    Current = null;
                    return false;
                }

                try
                {
                    ChannelMessage message =
                        await _channel.ReadAsync(_cancellationToken).ConfigureAwait(false);
                    object payload = _serializer.Deserialize(message.Message);
                    Current = new EventMessage(message.Channel, payload);
                    return true;
                }
                catch
                {
                    Current = null;
                    return false;
                }
            }

            public async ValueTask DisposeAsync()
            {
                await _channel.UnsubscribeAsync().ConfigureAwait(false);
                _isCompleted = true;
            }
        }
    }
}
