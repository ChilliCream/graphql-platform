using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Types;
using static HotChocolate.Subscriptions.InMemory.Properties.Resources;

namespace HotChocolate.Subscriptions.InMemory;

internal sealed class InMemorySourceStream<TMessage> : ISourceStream<TMessage>
{
    private readonly Channel<EventMessageEnvelope<TMessage>> _channel;
    private bool _read;
    private bool _disposed;

    public InMemorySourceStream(Channel<EventMessageEnvelope<TMessage>> channel)
    {
        _channel = channel;
    }

    public IAsyncEnumerable<TMessage> ReadEventsAsync()
    {
        if (_read)
        {
            throw new InvalidOperationException(
                InMemorySourceStream_ReadEventsAsync_ReadOnlyOnce);
        }

        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(InMemorySourceStream<TMessage>));
        }

        _read = true;
        return new EnumerateMessages<TMessage>(_channel);
    }

    IAsyncEnumerable<object> ISourceStream.ReadEventsAsync() =>
        new EnumerateMessages(ReadEventsAsync());

    public ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _channel.Writer.TryComplete();
            _disposed = true;
        }
        return default;
    }

    private sealed class EnumerateMessages<T> : IAsyncEnumerable<T>
    {
        private readonly Channel<EventMessageEnvelope<T>> _channel;

        public EnumerateMessages(Channel<EventMessageEnvelope<T>> channel)
        {
            _channel = channel;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(
            CancellationToken cancellationToken = default) =>
            new WrappedEnumerator<T>(
                GetAsyncEnumeratorInternally(cancellationToken),
                CompleteChannel);

        private async IAsyncEnumerator<T> GetAsyncEnumeratorInternally(
            CancellationToken cancellationToken = default)
        {
            while (await _channel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (_channel.Reader.TryRead(out var value))
                {
                    if (value.IsCompletedMessage)
                    {
                        _channel.Writer.TryComplete();
                        yield break;
                    }

                    yield return value.Body;
                }
            }
        }

        private ValueTask CompleteChannel()
        {
            _channel.Writer.TryComplete();
            return default;
        }
    }

    private sealed class EnumerateMessages : IAsyncEnumerable<object>
    {
        private readonly IAsyncEnumerable<TMessage> _messages;

        public EnumerateMessages(IAsyncEnumerable<TMessage> messages)
        {
            _messages = messages;
        }

        public async IAsyncEnumerator<object> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
        {
            await foreach (var message in _messages.WithCancellation(cancellationToken))
            {
                yield return message!;
            }
        }
    }

    private sealed class WrappedEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IAsyncEnumerator<T> _enumerator;
        private readonly Func<ValueTask> _dispose;

        public WrappedEnumerator(IAsyncEnumerator<T> enumerator, Func<ValueTask> dispose)
        {
            _enumerator = enumerator;
            _dispose = dispose;
        }

        public T Current => _enumerator.Current;

        public ValueTask<bool> MoveNextAsync() => _enumerator.MoveNextAsync();

        public async ValueTask DisposeAsync()
        {
            await _enumerator.DisposeAsync();
            await _dispose();
        }
    }
}
