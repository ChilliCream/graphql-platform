using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using HotChocolate.Execution;
using static HotChocolate.Subscriptions.Properties.Resources;

namespace HotChocolate.Subscriptions.InMemory
{
    public class InMemorySourceStream<TMessage> : ISourceStream<TMessage>
    {
        private readonly Channel<TMessage> _channel;
        private bool _read;
        private bool _disposed;

        public InMemorySourceStream(Channel<TMessage> channel)
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

        private class EnumerateMessages<T> : IAsyncEnumerable<T>
        {
            private readonly Channel<T> _channel;

            public EnumerateMessages(Channel<T> channel)
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
                while (await _channel.Reader.WaitToReadAsync(cancellationToken)
                    .ConfigureAwait(false))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        yield break;
                    }

                    yield return await _channel.Reader.ReadAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
            }

            private ValueTask CompleteChannel()
            {
                // no more readers, outgoing channel should be closed
                _channel.Writer.TryComplete();
                return default;
            }
        }

        private class EnumerateMessages: IAsyncEnumerable<object>
        {
            private readonly IAsyncEnumerable<TMessage> _messages;

            public EnumerateMessages(IAsyncEnumerable<TMessage> messages)
            {
                _messages = messages;
            }

            public async IAsyncEnumerator<object> GetAsyncEnumerator(
                CancellationToken cancellationToken = default)
            {
                await foreach (TMessage message in _messages.WithCancellation(cancellationToken))
                {
                    yield return message!;
                }
            }
        }

        private class WrappedEnumerator<T> : IAsyncEnumerator<T>
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
}
