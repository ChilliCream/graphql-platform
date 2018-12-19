using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Subscriptions;

namespace HotChocolate.Execution
{
    internal delegate Task<IQueryExecutionResult> ExecuteSubscriptionQuery(
        IExecutionContext executionContext,
        CancellationToken cancellationToken);

    internal class SubscriptionResult
        : ISubscriptionExecutionResult
    {
        private readonly IEventStream _eventStream;
        private readonly Func<IEventMessage, IExecutionContext> _contextFactory;
        private readonly ExecuteSubscriptionQuery _executeQuery;
        private readonly CancellationTokenSource _cancellationTokenSource =
            new CancellationTokenSource();
        private bool _isCompleted;

        public SubscriptionResult(
            IEventStream eventStream,
            Func<IEventMessage, IExecutionContext> contextFactory,
            ExecuteSubscriptionQuery executeQuery)
        {
            _eventStream = eventStream
                ?? throw new ArgumentNullException(nameof(eventStream));
            _contextFactory = contextFactory
                ?? throw new ArgumentNullException(nameof(contextFactory));
            _executeQuery = executeQuery
                ?? throw new ArgumentNullException(nameof(executeQuery));
        }

        public IReadOnlyCollection<IError> Errors { get; }

        public bool IsCompleted => _isCompleted && _eventStream.IsCompleted;

        public Task<IQueryExecutionResult> ReadAsync()
        {
            return ReadAsync(CancellationToken.None);
        }

        public async Task<IQueryExecutionResult> ReadAsync(
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

        private async Task<IQueryExecutionResult> ExecuteQueryAsync(
            IEventMessage message,
            CancellationToken cancellationToken)
        {
            using (IExecutionContext context = _contextFactory(message))
            {
                return await _executeQuery(context, cancellationToken);
            }
        }

        public void Dispose()
        {
            _isCompleted = true;
            _cancellationTokenSource.Cancel();
            _eventStream.Dispose();
            _cancellationTokenSource.Dispose();
        }
    }


}
