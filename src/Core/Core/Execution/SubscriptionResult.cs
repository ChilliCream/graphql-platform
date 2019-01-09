using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Subscriptions;

namespace HotChocolate.Execution
{
    internal delegate Task<IReadOnlyQueryResult> ExecuteSubscriptionQuery(
        IExecutionContext executionContext,
        CancellationToken cancellationToken);

    internal class SubscriptionResult
        : ISubscriptionExecutionResult
    {
        private readonly IEventStream _eventStream;
        private readonly Func<IEventMessage, IExecutionContext> _contextFactory;
        private readonly ExecuteSubscriptionQuery _executeQuery;
        private readonly IRequestServiceScope _serviceScope;
        private readonly CancellationTokenSource _cancellationTokenSource =
            new CancellationTokenSource();
        private bool _isCompleted;

        public SubscriptionResult(
            IEventStream eventStream,
            Func<IEventMessage, IExecutionContext> contextFactory,
            ExecuteSubscriptionQuery executeQuery,
            IRequestServiceScope serviceScope)
        {
            _eventStream = eventStream
                ?? throw new ArgumentNullException(nameof(eventStream));
            _contextFactory = contextFactory
                ?? throw new ArgumentNullException(nameof(contextFactory));
            _executeQuery = executeQuery
                ?? throw new ArgumentNullException(nameof(executeQuery));
            _serviceScope = serviceScope
                ?? throw new ArgumentNullException(nameof(serviceScope));
            _serviceScope.HandleLifetime();
        }

        public IReadOnlyCollection<IError> Errors { get; }

        public IReadOnlyDictionary<string, object> Extensions { get; } =
            new OrderedDictionary();

        public bool IsCompleted => _isCompleted && _eventStream.IsCompleted;

        public Task<IReadOnlyQueryResult> ReadAsync()
        {
            return ReadAsync(CancellationToken.None);
        }

        public async Task<IReadOnlyQueryResult> ReadAsync(
            CancellationToken cancellationToken)
        {
            if (IsCompleted)
            {
                throw new InvalidOperationException(
                    "The response stream has already been completed.");
            }

            using (var ct = CancellationTokenSource.CreateLinkedTokenSource(
                _cancellationTokenSource.Token, cancellationToken))
            {
                IEventMessage message = await _eventStream.ReadAsync(ct.Token);
                return await ExecuteQueryAsync(message, ct.Token);
            }
        }

        private async Task<IReadOnlyQueryResult> ExecuteQueryAsync(
            IEventMessage message,
            CancellationToken cancellationToken)
        {
            IExecutionContext context = _contextFactory(message);
            return await _executeQuery(context, cancellationToken);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isCompleted)
            {
                if (disposing)
                {
                    _cancellationTokenSource.Cancel();
                    _serviceScope.Dispose();
                    _eventStream.Dispose();
                    _cancellationTokenSource.Dispose();
                }
                _isCompleted = true;
            }
        }
    }
}
