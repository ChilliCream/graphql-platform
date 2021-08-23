using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Subscriptions.Messages;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using static HotChocolate.AspNetCore.ErrorHelper;

namespace HotChocolate.AspNetCore.Subscriptions
{
    internal sealed class SubscriptionSession : ISubscriptionSession
    {
        internal const byte Delimiter = 0x07;
        private readonly CancellationTokenSource _session;
        private readonly CancellationToken _sessionToken;
        private readonly ISocketConnection _connection;
        private readonly IResponseStream _responseStream;
        private readonly IDiagnosticEvents _diagnosticEvents;
        private bool _disposed;

        /// <inheritdoc />
        public event EventHandler? Completed;

        public SubscriptionSession(
            CancellationTokenSource session,
            ISocketConnection connection,
            IResponseStream responseStream,
            ISubscription subscription,
            IDiagnosticEvents diagnosticEvents,
            string clientSubscriptionId)
        {
            _session = session ??
                throw new ArgumentNullException(nameof(session));
            _connection = connection ??
                throw new ArgumentNullException(nameof(connection));
            _responseStream = responseStream ??
                throw new ArgumentNullException(nameof(responseStream));
            _diagnosticEvents = diagnosticEvents ??
                throw new ArgumentNullException(nameof(diagnosticEvents));
            Subscription = subscription ??
                throw new ArgumentNullException(nameof(subscription));
            Id = clientSubscriptionId ??
                throw new ArgumentNullException(nameof(clientSubscriptionId));

            _sessionToken = _session.Token;

            Task.Factory.StartNew(
                SendResultsAsync,
                _sessionToken,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        /// <inheritdoc />
        public string Id { get; }

        /// <inheritdoc />
        public ISubscription Subscription { get; }

        private async Task SendResultsAsync()
        {
            await using IResponseStream responseStream = _responseStream;
            CancellationToken cancellationToken = _sessionToken;

            try
            {
                await foreach (IQueryResult result in
                    responseStream.ReadResultsAsync().WithCancellation(cancellationToken))
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

                _diagnosticEvents.SubscriptionTransportError(Subscription, ex);
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
                if (!_session.IsCancellationRequested)
                {
                    _session.Cancel();
                }

                _session.Dispose();
                _disposed = true;
            }
        }
    }
}
