using System;
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

        public bool IsCompleted => _channel.Reader.Completion.IsCompleted;

        public ValueTask TriggerAsync(
            IEventMessage message,
            CancellationToken cancellationToken = default)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            return _channel.Writer.WriteAsync(message, cancellationToken);
        }

        public ValueTask<IEventMessage> ReadAsync(
            CancellationToken cancellationToken = default)
        {
            return _channel.Reader.ReadAsync(cancellationToken);
        }

        public ValueTask CompleteAsync(CancellationToken cancellationToken = default)
        {
            _channel.Writer.Complete();
            Completed?.Invoke(this, EventArgs.Empty);
            return default;
        }

        #region IDisposable

        private bool _disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (!_channel.Reader.Completion.IsCompleted)
                    {
                        _channel.Writer.Complete();
                    }
                    Completed?.Invoke(this, EventArgs.Empty);
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}

