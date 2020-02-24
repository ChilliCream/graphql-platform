using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

#nullable enable

namespace HotChocolate.Subscriptions.InMemory
{
    public class InMemoryEventStream<TMessage>
        : IEventStream<TMessage>
    {
        private readonly Channel<TMessage> _channel;
        private InMemoryEventStreamEnumerator? _enumerator;

        public InMemoryEventStream(Channel<TMessage> channel)
        {
            _channel = channel;
        }

        public IAsyncEnumerator<TMessage> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
        {
            if (_channel.Reader.Completion.IsCompleted)
            {
                throw new InvalidOperationException(
                    "The stream has been completed and cannot be replayed.");
            }

            if (_enumerator is null)
            {
                _enumerator = new InMemoryEventStreamEnumerator(
                    _channel,
                    cancellationToken);
            }

            return _enumerator;
        }

        private class InMemoryEventStreamEnumerator
            : IAsyncEnumerator<TMessage>
        {
            private readonly Channel<TMessage> _channel;
            private readonly CancellationToken _cancellationToken;
            private bool _disposed;

            public InMemoryEventStreamEnumerator(
                Channel<TMessage> channel,
                CancellationToken cancellationToken)
            {
                _channel = channel;
                _cancellationToken = cancellationToken;
                Current = default!;
            }

            // [AllowNull]
            // [MaybeNull]
            public TMessage Current { get; private set; }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (_disposed || _channel.Reader.Completion.IsCompleted)
                {
                    Current = default!;
                    return false;
                }

                try
                {
                    if (await _channel.Reader.WaitToReadAsync(
                        _cancellationToken)
                        .ConfigureAwait(false))
                    {
                        Current = await _channel.Reader.ReadAsync(
                            _cancellationToken)
                            .ConfigureAwait(false);
                        return true;
                    }

                    Current = default!;
                    return false;
                }
                catch
                {
                    Current = default!;
                    return false;
                }
            }

            public ValueTask DisposeAsync()
            {
                if (!_disposed)
                {
                    if (!_channel.Reader.Completion.IsCompleted)
                    {
                        _channel.Writer.TryComplete();
                    }
                    _disposed = true;
                }
                return default;
            }
        }
    }
}
