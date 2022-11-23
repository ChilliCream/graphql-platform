using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using StackExchange.Redis;

namespace HotChocolate.Subscriptions.Redis;

internal sealed class RedisSourceStream<TMessage> : ISourceStream<TMessage>
{
    private readonly ChannelMessageQueue _channel;
    private readonly IMessageSerializer _messageSerializer;
    private bool _read;
    private bool _disposed;

    public RedisSourceStream(ChannelMessageQueue channel, IMessageSerializer messageSerializer)
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
            throw new ObjectDisposedException(nameof(RedisSourceStream<TMessage>));
        }

        _read = true;
        return new EnumerateMessages<TMessage>(_channel, _messageSerializer);
    }

    IAsyncEnumerable<object> ISourceStream.ReadEventsAsync()
    {
        if (_read)
        {
            throw new InvalidOperationException("This stream can only be read once.");
        }

        if (_disposed || _channel.Completion.IsCompleted)
        {
            throw new ObjectDisposedException(nameof(RedisSourceStream<TMessage>));
        }

        _read = true;
        return new EnumerateMessagesAsObject<TMessage>(_channel, _messageSerializer);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await _channel.UnsubscribeAsync().ConfigureAwait(false);
            _disposed = true;
        }
    }

    private sealed class EnumerateMessages<T> : IAsyncEnumerable<T>
    {
        private readonly ChannelMessageQueue _channel;
        private readonly IMessageSerializer _messageSerializer;

        public EnumerateMessages(
            ChannelMessageQueue channel,
            IMessageSerializer messageSerializer)
        {
            _channel = channel;
            _messageSerializer = messageSerializer;
        }

        public async IAsyncEnumerator<T> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var cm = await _channel.ReadAsync(cancellationToken).ConfigureAwait(false);
                var em = _messageSerializer.Deserialize<EventMessage<T>>(cm.Message!);

                if (em.IsCompletedMessage)
                {
                    yield break;
                }

                yield return em.Body;
            }
        }
    }

    private sealed class EnumerateMessagesAsObject<T> : IAsyncEnumerable<object>
    {
        private readonly ChannelMessageQueue _channel;
        private readonly IMessageSerializer _messageSerializer;

        public EnumerateMessagesAsObject(
            ChannelMessageQueue channel,
            IMessageSerializer messageSerializer)
        {
            _channel = channel;
            _messageSerializer = messageSerializer;
        }

        public async IAsyncEnumerator<object> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var cm = await _channel.ReadAsync(cancellationToken).ConfigureAwait(false);
                var em = _messageSerializer.Deserialize<EventMessage<T>>(cm.Message!);

                if (em.IsCompletedMessage)
                {
                    yield break;
                }

                yield return em.Body;
            }
        }
    }
}
