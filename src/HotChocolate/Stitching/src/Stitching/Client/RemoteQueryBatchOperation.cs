using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;
using HotChocolate.Execution;

namespace HotChocolate.Stitching.Client
{
    public sealed class RemoteQueryBatchOperation
        // : IBatchOperation
        : IObserver<IRemoteQueryClient>
        , IDisposable
    {
        private readonly object _sync = new object();
        private readonly HashSet<IRemoteQueryClient> _clients =
            new HashSet<IRemoteQueryClient>();
        private readonly IDisposable _subscription;
        private bool _disposed;
        private int _bufferSize;

        public int BufferSize => _bufferSize;

        public event EventHandler<EventArgs> BufferedRequests;

        public RemoteQueryBatchOperation(IStitchingContext context)
        {
            _subscription = context.Subscribe(this);
        }

        public async Task InvokeAsync(CancellationToken cancellationToken)
        {
            if (_bufferSize > 0)
            {
                await Task.WhenAll(_clients.Select(t =>
                   t.DispatchAsync(cancellationToken)))
                   .ConfigureAwait(false);
            }

            UpdateBufferSize();
        }

        public void OnNext(IRemoteQueryClient value)
        {
            lock (_sync)
            {
                _bufferSize += value.BufferSize;
                if (_clients.Add(value))
                {
                    value.BufferedRequest += RequestBuffered;
                }
            }
        }

        public void OnCompleted()
        {
            lock (_sync)
            {
                foreach (IRemoteQueryClient client in _clients)
                {
                    client.BufferedRequest -= RequestBuffered;
                }
                _clients.Clear();
                _bufferSize = 0;
            }
        }

        public void OnError(Exception error)
        {
            // we do not care about errors here.
        }

        public void RequestBuffered(
            IRemoteQueryClient sender,
            EventArgs eventArgs)
        {
            lock (_sync)
            {
                _bufferSize++;
            }
            BufferedRequests.Invoke(this, EventArgs.Empty);
        }

        public void UpdateBufferSize()
        {
            lock (_sync)
            {
                _bufferSize = 0;
                foreach (IRemoteQueryClient client in _clients)
                {
                    _bufferSize += client.BufferSize;
                }
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _subscription.Dispose();
                _disposed = true;
            }
        }
    }
}
