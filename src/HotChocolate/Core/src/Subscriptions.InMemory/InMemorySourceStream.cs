using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using HotChocolate.Execution;

namespace HotChocolate.Subscriptions.InMemory
{
    public class InMemorySourceStream<TMessage>
        : ISourceStream<TMessage>
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
                throw new InvalidOperationException("This stream can only be read once.");
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
                CancellationToken cancellationToken = default)
            {
                return new WrappedEnumerator<T>(GetAsyncEnumeratorInternally(cancellationToken), CompleteChannel);
            }

            public async IAsyncEnumerator<T> GetAsyncEnumeratorInternally(
                CancellationToken cancellationToken = default)
            {
                while (await _channel.Reader.WaitToReadAsync(cancellationToken))
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
            private readonly Func<ValueTask> _disposingAction;

            public WrappedEnumerator(IAsyncEnumerator<T> enumerator, Func<ValueTask> disposingAction)
            {
                _enumerator = enumerator;
                _disposingAction = disposingAction;
            }

            public async ValueTask DisposeAsync()
            {
                await _enumerator.DisposeAsync();
                await _disposingAction();
            }

            public ValueTask<bool> MoveNextAsync()
            {
                return _enumerator.MoveNextAsync();
            }

            public T Current => _enumerator.Current;
        }
    }
}
