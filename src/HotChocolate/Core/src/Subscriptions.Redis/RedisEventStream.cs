using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using StackExchange.Redis;

namespace HotChocolate.Subscriptions.Redis
{
    public class RedisEventStream<TMessage>
        : ISourceStream<TMessage>
    {
        private readonly ChannelMessageQueue _channel;
        private readonly IMessageSerializer _messageSerializer;
        private bool _read;
        private bool _disposed;

        public RedisEventStream(ChannelMessageQueue channel, IMessageSerializer messageSerializer)
        {
            _channel = channel;
            _messageSerializer = messageSerializer;
        }

        public IAsyncEnumerable<TMessage> ReadEventsAsync()
        {
            if (_read)
            {
                throw new InvalidOperationException("This stream can only be read once.");
            }

            if (_disposed || _channel.Completion.IsCompleted)
            {
                throw new ObjectDisposedException(nameof(RedisEventStream<TMessage>));
            }

            _read = true;
            return new EnumerateMessages<TMessage>(
                _channel,
                _messageSerializer.Deserialize<TMessage>);
        }

        IAsyncEnumerable<object> ISourceStream.ReadEventsAsync()
        {
            if (_read)
            {
                throw new InvalidOperationException("This stream can only be read once.");
            }

            if (_disposed || _channel.Completion.IsCompleted)
            {
                throw new ObjectDisposedException(nameof(RedisEventStream<TMessage>));
            }

            _read = true;
            return new EnumerateMessages<object>(
                _channel,
                s => _messageSerializer.Deserialize<TMessage>(s));
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                await _channel.UnsubscribeAsync().ConfigureAwait(false);
                _disposed = true;
            }
        }

        private class EnumerateMessages<T>
            : IAsyncEnumerable<T>
        {
            private readonly ChannelMessageQueue _channel;
            private readonly Func<string, T> _messageSerializer;

            public EnumerateMessages(
                ChannelMessageQueue channel,
                Func<string, T> messageSerializer)
            {
                _channel = channel;
                _messageSerializer = messageSerializer;
            }

            public async IAsyncEnumerator<T> GetAsyncEnumerator(
                CancellationToken cancellationToken = default)
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    ChannelMessage message = await _channel.ReadAsync(cancellationToken)
                        .ConfigureAwait(false);
                    string body = message.Message;

                    if (body.Equals(RedisPubSub.Completed, StringComparison.Ordinal))
                    {
                        yield break;
                    }

                    yield return _messageSerializer(message.Message);
                }
            }
        }
    }
}
