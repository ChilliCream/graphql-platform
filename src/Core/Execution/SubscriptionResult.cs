using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Subscriptions;

namespace HotChocolate.Execution
{
    internal class SubscriptionResult
        : ISubscriptionExecutionResult
    {
        private readonly IEventStream _eventStream;
        private readonly IExecutionContext _executionContext;
        private readonly ExecuteSubscriptionQuery _executeQuery;
        private readonly CancellationTokenSource _cancellationTokenSource =
            new CancellationTokenSource();

        public SubscriptionResult(
            IEventStream eventStream,
            IExecutionContext executionContext,
            ExecuteSubscriptionQuery executeQuery)
        {
            _eventStream = eventStream
                ?? throw new ArgumentNullException(nameof(eventStream));
            _executionContext = executionContext
                ?? throw new ArgumentNullException(nameof(executionContext));
            _executeQuery = executeQuery
                ?? throw new ArgumentNullException(nameof(executeQuery));
        }

        public IQueryExecutionResult Current { get; private set; }

        public IReadOnlyCollection<IQueryError> Errors { get; }

        public async Task<bool> MoveNextAsync(
            CancellationToken cancellationToken = default)
        {
            if (_eventStream.IsCompleted)
            {
                return false;
            }

            try
            {
                using (var ct = CancellationTokenSource.CreateLinkedTokenSource(
                    _cancellationTokenSource.Token, cancellationToken))
                {
                    await _eventStream.NextAsync(ct.Token);
                    Current = await _executeQuery(_executionContext, ct.Token);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _eventStream.Dispose();
            _executionContext.Dispose();
            _cancellationTokenSource.Dispose();
        }
    }

    internal delegate Task<IQueryExecutionResult> ExecuteSubscriptionQuery(
        IExecutionContext executionContext,
        CancellationToken cancellationToken);
}
