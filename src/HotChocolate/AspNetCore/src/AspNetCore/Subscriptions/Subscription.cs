using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Subscriptions.Messages;
using HotChocolate.Execution;
using static HotChocolate.AspNetCore.ErrorHelper;

namespace HotChocolate.AspNetCore.Subscriptions
{
    internal sealed class Subscription : ISubscription
    {
        internal const byte Delimiter = 0x07;
        private readonly CancellationTokenSource _cts;
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

            _cts = CancellationTokenSource.CreateLinkedTokenSource(_connection.RequestAborted);

            Task.Factory.StartNew(
                SendResultsAsync,
                _cts.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        public string Id { get; }

        private async Task SendResultsAsync()
        {
            CancellationToken cancellationToken = _cts.Token;

            try
            {
                await foreach (IQueryResult result in
                    _responseStream.ReadResultsAsync().WithCancellation(cancellationToken))
                {
                    using (result)
                    {
                        if (!cancellationToken.IsCancellationRequested && !_connection.Closed)
                        {
                            await _connection.SendAsync(
                                new DataResultMessage(Id, result),
                                cancellationToken);
                        }
                    }
                }

                if (!cancellationToken.IsCancellationRequested && !_connection.Closed)
                {
                    await _connection.SendAsync(new DataCompleteMessage(Id), cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
            catch (ObjectDisposedException) { }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                if (!_connection.Closed)
                {
                    try
                    {
                        try
                        {
                            await _connection.SendAsync(
                                new DataResultMessage(Id, UnknownSubscriptionError(ex)),
                                cancellationToken);
                        }
                        finally
                        {
                            await _connection.SendAsync(
                                new DataCompleteMessage(Id),
                                cancellationToken);
                        }
                    }
                    catch
                    {
                        // suppress all errors, so original exception can be rethrown
                    }
                }

                // original exception should be propagated to upper level in order to be logged
                // correctly at least.
                throw;
            }
            finally
            {
                // completed should be always invoked to be ensure that disposed subscription is
                // removed from subscription manager
                Completed?.Invoke(this, EventArgs.Empty);
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
