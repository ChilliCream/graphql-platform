using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace HotChocolate.Subscriptions
{
    [Obsolete]
    public class InMemoryEventStream
        : IEventStream
    {
        private readonly Channel<IEventMessage> _channel;
        private InMemoryEventStreamEnumerator _enumerator;
        private bool _isCompleted;

        public InMemoryEventStream()
        {
            _channel = Channel.CreateUnbounded<IEventMessage>();
        }

        public event EventHandler Completed;

        public IAsyncEnumerator<IEventMessage> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
        {
            if (_isCompleted || (_enumerator is { IsCompleted: true }))
            {
                throw new InvalidOperationException(
                    "The stream has been completed and cannot be replayed.");
            }

            if (_enumerator is null)
            {
                _enumerator = new InMemoryEventStreamEnumerator(
                    _channel,
                    () => Completed?.Invoke(this, EventArgs.Empty),
                    cancellationToken);
            }

            return _enumerator;
        }

        public ValueTask TriggerAsync(
            IEventMessage message,
            CancellationToken cancellationToken = default)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return TriggerInternalAsync(message, cancellationToken);
        }

        private async ValueTask TriggerInternalAsync(
            IEventMessage message,
            CancellationToken cancellationToken = default)
        {
            if (await _channel.Writer.WaitToWriteAsync(cancellationToken).ConfigureAwait(false))
            {
                await _channel.Writer.WriteAsync(message, cancellationToken);
            }
        }

        public ValueTask CompleteAsync(CancellationToken cancellationToken = default)
        {
            if (!_isCompleted)
            {
                _channel.Writer.Complete();
                Completed?.Invoke(this, EventArgs.Empty);
                _isCompleted = true;
            }
            return default;
        }

        private class InMemoryEventStreamEnumerator
            : IAsyncEnumerator<IEventMessage>
        {
            private readonly Channel<IEventMessage> _channel;
            private readonly Action _completed;
            private readonly CancellationToken _cancellationToken;
            private bool _isCompleted;

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

            internal bool IsCompleted => _isCompleted;

            public async ValueTask<bool> MoveNextAsync()
            {
                if (_isCompleted || _channel.Reader.Completion.IsCompleted)
                {
                    return false;
                }

                try
                {
                    if (await _channel.Reader.WaitToReadAsync(_cancellationToken)
                        .ConfigureAwait(false))
                    {
                        Current = await _channel.Reader.ReadAsync(_cancellationToken)
                            .ConfigureAwait(false);
                        return true;
                    }

                    Current = null;
                    return false;
                }
                catch
                {
                    Current = null;
                    return false;
                }
            }

            public ValueTask DisposeAsync()
            {
                if (!_isCompleted)
                {
                    if (!_channel.Reader.Completion.IsCompleted)
                    {
                        _channel.Writer.Complete();
                    }
                    _completed();
                    _isCompleted = true;
                }
                return default;
            }
        }
    }
}
