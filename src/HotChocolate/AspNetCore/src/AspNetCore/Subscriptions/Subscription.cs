using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Subscriptions.Messages;
using HotChocolate.Execution;

namespace HotChocolate.AspNetCore.Subscriptions
{
    internal sealed class Subscription : ISubscription
    {
        internal const byte Delimiter = 0x07;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly ISocketConnection _connection;
        private readonly IResponseStream _responseStream;
        private bool _disposed;

        public event EventHandler? Completed;

        public Subscription(
            ISocketConnection connection,
            IResponseStream responseStream,
            string id)
        {
            _connection = connection ??
                throw new ArgumentNullException(nameof(connection));
            _responseStream = responseStream ??
                throw new ArgumentNullException(nameof(responseStream));
            Id = id ??
                throw new ArgumentNullException(nameof(id));

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
                await foreach (IQueryResult result in
                    _responseStream.ReadResultsAsync().WithCancellation(_cts.Token))
                {
                    using (result)
                    {
                        await _connection.SendAsync(new DataResultMessage(Id, result), _cts.Token);
                    }
                }

                if (!_cts.IsCancellationRequested)
                {
                    await _connection.SendAsync(new DataCompleteMessage(Id), _cts.Token);
                    Completed?.Invoke(this, EventArgs.Empty);
                }
            }
            catch(OperationCanceledException){}
            catch(ObjectDisposedException){}
            catch (Exception ex)
            {
                if (!_cts.IsCancellationRequested)
                {
                    //TODO Send error
                    await _connection.SendAsync(new DataErrorMessage(Id), _cts.Token);
                    Completed?.Invoke(this, EventArgs.Empty);
                }
            }
            finally
            {
                Dispose();
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (!_cts.IsCancellationRequested)
                {
                    _cts.Cancel();
                }
                _cts.Dispose();
                _disposed = true;
            }
        }
    }
}
