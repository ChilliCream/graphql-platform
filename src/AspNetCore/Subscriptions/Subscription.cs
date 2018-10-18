using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public class Subscription
        : ISubscription
    {
        private readonly CancellationTokenSource _cts =
            new CancellationTokenSource();
        private readonly IWebSocketContext _context;
        private readonly IResponseStream _responseStream;
        private bool _disposed;

        public event EventHandler Completed;

        public Subscription(
            IWebSocketContext context,
            IResponseStream responseStream,
            string id)
        {
            _context = context
                ?? throw new ArgumentNullException(nameof(context));
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
                IQueryExecutionResult result =
                    await _responseStream.ReadAsync(_cts.Token);

                if (result != null)
                {
                    await _context.SendSubscriptionDataMessageAsync(
                        Id, result, _cts.Token);
                }
            }

            if (_responseStream.IsCompleted && !_cts.IsCancellationRequested)
            {
                await _context.SendSubscriptionCompleteMessageAsync(
                    Id, _cts.Token);

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
