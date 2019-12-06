using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace HotChocolate.Subscriptions
{
    public class InMemoryEventStream
        : IEventStream
    {
        private readonly Channel<IEventMessage> _channel;

        public InMemoryEventStream()
        {
            _channel = Channel.CreateUnbounded<IEventMessage>();
        }

        public event EventHandler Completed;

        public IAsyncEnumerator<IEventMessage> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
        {
            return new InMemoryEventStreamEnumerator(
                _channel,
                () => Completed?.Invoke(this, EventArgs.Empty),
                cancellationToken);
        }

        public async ValueTask TriggerAsync(
            IEventMessage message,
            CancellationToken cancellationToken = default)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (await _channel.Writer.WaitToWriteAsync(cancellationToken).ConfigureAwait(false))
            {
                await _channel.Writer.WriteAsync(message, cancellationToken);
            }
        }

        public ValueTask CompleteAsync(CancellationToken cancellationToken = default)
        {
            _channel.Writer.Complete();
            Completed?.Invoke(this, EventArgs.Empty);
            return default;
        }

        private class InMemoryEventStreamEnumerator
            : IAsyncEnumerator<IEventMessage>
        {
            private readonly Channel<IEventMessage> _channel;
            private readonly Action _completed;
            private readonly CancellationToken _cancellationToken;
            private bool _disposedValue;

            public InMemoryEventStreamEnumerator(
                Channel<IEventMessage> channel,
                Action completed,
                CancellationToken cancellationToken)
            {
                _channel = channel;
                _completed = completed;
                _cancellationToken = cancellationToken;
            }

            public IEventMessage Current { get; private set; }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (await _channel.Reader.WaitToReadAsync(_cancellationToken).ConfigureAwait(false))
                {
                    Current = await _channel.Reader.ReadAsync(_cancellationToken);
                    return true;
                }

                Current = null;
                return false;
            }

            public ValueTask DisposeAsync()
            {
                if (!_disposedValue)
                {
                    if (!_channel.Reader.Completion.IsCompleted)
                    {
                        _channel.Writer.Complete();
                    }
                    _completed();
                    _disposedValue = true;
                }
                return default;
            }
        }
    }
}
