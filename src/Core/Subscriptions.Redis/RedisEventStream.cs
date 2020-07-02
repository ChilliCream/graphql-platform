using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace HotChocolate.Subscriptions.Redis
{
    public class RedisEventStream<TMessage>
        : IEventStream<TMessage>
    {
        private readonly ChannelMessageQueue _channel;
        private readonly IMessageSerializer _messageSerializer;
        private RedisEventStreamEnumerator _enumerator;
        private bool _isCompleted;

        public RedisEventStream(
            ChannelMessageQueue channel, 
            IMessageSerializer messageSerializer)
        {
            _channel = channel;
            _messageSerializer = messageSerializer;
        }

        public IAsyncEnumerator<TMessage> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
        {
            if (_isCompleted || (_enumerator is { IsCompleted: true }))
            {
                throw new InvalidOperationException(
                    "The stream has been completed and cannot be replayed.");
            }

            if (_enumerator is null)
            {
                _enumerator = new RedisEventStreamEnumerator(
                    _channel,
                    _messageSerializer,
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
            : IAsyncEnumerator<TMessage>
        {
            private readonly ChannelMessageQueue _channel;
            private readonly IMessageSerializer _messageSerializer;
            private readonly CancellationToken _cancellationToken;
            private bool _isCompleted;

            public RedisEventStreamEnumerator(
                ChannelMessageQueue channel,
                IMessageSerializer messageSerializer,
                CancellationToken cancellationToken)
            {
                _channel = channel;
                _messageSerializer = messageSerializer;
                _cancellationToken = cancellationToken;
            }

            public TMessage Current { get; private set; }

            internal bool IsCompleted => _isCompleted;

            public async ValueTask<bool> MoveNextAsync()
            {
                if (_isCompleted || _channel.Completion.IsCompleted)
                {
                    Current = default;
                    return false;
                }

                try
                {
                    ChannelMessage message =
                        await _channel.ReadAsync(_cancellationToken).ConfigureAwait(false);
                    string body = message.Message;

                    if (body.Equals(RedisPubSub.Completed, StringComparison.Ordinal))
                    {
                        Current = default;
                        return false;
                    }

                    Current = _messageSerializer.Deserialize<TMessage>(body);
                    return true;
                }
                catch
                {
                    Current = default;
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
