using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Subscriptions.Messages;
using HotChocolate.Execution;
using HotChocolate.Server;

namespace HotChocolate.AspNetCore.Subscriptions
{
    internal class Subscription
        : ISubscription
    {
        internal const byte _delimiter = 0x07;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly ISocketConnection _connection;
        private readonly IResponseStream _responseStream;
        private bool _disposed;

        public event EventHandler Completed;

        public Subscription(
            ISocketConnection connection,
            IResponseStream responseStream,
            string id)
        {
            _connection = connection
                ?? throw new ArgumentNullException(nameof(connection));
            _responseStream = responseStream
                ?? throw new ArgumentNullException(nameof(responseStream));
            Id = id
                ?? throw new ArgumentNullException(nameof(id));

            Task.Factory.StartNew(
                SendResultsAsync,
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        public string Id { get; }

        private async Task SendResultsAsync()
        {
            try
            {
                while (!_responseStream.IsCompleted
                    && !_cts.IsCancellationRequested)
                {
                    IReadOnlyQueryResult result =
                        await _responseStream.ReadAsync(_cts.Token)
                            .ConfigureAwait(false);

                    if (result != null)
                    {
                        await _connection.SendAsync(
                            new DataResultMessage(Id, result).Serialize(),
                            _cts.Token)
                            .ConfigureAwait(false);
                    }
                }

                if (_responseStream.IsCompleted
                    && !_cts.IsCancellationRequested)
                {
                    await _connection.SendAsync(
                        new DataCompleteMessage(Id).Serialize(),
                        _cts.Token).ConfigureAwait(false);

                    Completed?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (TaskCanceledException)
            {
                // the subscription was canceled.
            }
            finally
            {
                Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (!_cts.IsCancellationRequested)
                    {
                        _cts.Cancel();
                    }
                    _responseStream.Dispose();
                    _cts.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
