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
        private readonly CancellationTokenSource _cts =
            new CancellationTokenSource();
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

            Task.Run(SendResultsAsync);
        }

        public string Id { get; }

        private async Task SendResultsAsync()
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
                        new QueryResultMessage(Id, result).Serialize(),
                        _cts.Token)
                        .ConfigureAwait(false);
                }
            }

            if (_responseStream.IsCompleted && !_cts.IsCancellationRequested)
            {
                await _connection.SendSubscriptionCompleteMessageAsync(
                    Id, _cts.Token).ConfigureAwait(false);

                Completed?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _cts.Cancel();
                _responseStream.Dispose();
                _cts.Dispose();
            }
        }
    }
}
