using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    internal delegate ValueTask<IReadOnlyQueryResult> ExecuteSubscriptionQuery(
        IExecutionContext executionContext,
        CancellationToken cancellationToken);

    internal sealed class SubscriptionResult
        : ISubscriptionExecutionResult
    {
        private readonly Dictionary<string, object> _contextData =
            new Dictionary<string, object>();
        private readonly SubscriptionResultEnumerator _enumerator;

        public SubscriptionResult(
            IAsyncEnumerable<object> sourceStream,
            Func<object, IExecutionContext> contextFactory,
            ExecuteSubscriptionQuery executeQuery,
            IRequestServiceScope serviceScope,
            CancellationToken cancellationToken)
        {
            _enumerator = new SubscriptionResultEnumerator(
                sourceStream,
                contextFactory,
                executeQuery,
                serviceScope,
                cancellationToken);
        }

        public IReadOnlyCollection<IError> Errors { get; }

        public IReadOnlyDictionary<string, object> Extensions { get; } =
            new OrderedDictionary();

        public IDictionary<string, object> ContextData => _contextData;

        IReadOnlyDictionary<string, object> IExecutionResult.ContextData =>
            _contextData;

        public IAsyncEnumerator<IReadOnlyQueryResult> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
        {
            if (_enumerator.IsCompleted)
            {
                throw new InvalidOperationException(
                    "The stream has been completed and cannot be replayed");
            }
            return _enumerator;
        }

        private sealed class SubscriptionResultEnumerator
            : IAsyncEnumerator<IReadOnlyQueryResult>
        {
            private readonly ConfiguredCancelableAsyncEnumerable<object>.Enumerator _sourceStream;
            private readonly CancellationToken _cancellationToken;
            private readonly Func<object, IExecutionContext> _contextFactory;
            private readonly ExecuteSubscriptionQuery _executeQuery;
            private readonly IRequestServiceScope _serviceScope;
            private bool _disposed;

            public SubscriptionResultEnumerator(
                IAsyncEnumerable<object> sourceStream,
                Func<object, IExecutionContext> contextFactory,
                ExecuteSubscriptionQuery executeQuery,
                IRequestServiceScope serviceScope,
                CancellationToken cancellationToken)
            {
                _sourceStream = sourceStream
                    .WithCancellation(cancellationToken)
                    .GetAsyncEnumerator();
                _contextFactory = contextFactory;
                _executeQuery = executeQuery;
                _serviceScope = serviceScope;
                _cancellationToken = cancellationToken;
                serviceScope.HandleLifetime();
            }

            public IReadOnlyQueryResult Current { get; private set; }

            internal bool IsCompleted => _disposed;

            public async ValueTask<bool> MoveNextAsync()
            {
                if (_disposed)
                {
                    return false;
                }

                bool hasResult = await _sourceStream.MoveNextAsync();

                if (hasResult)
                {
                    Current = await ExecuteQueryAsync(_sourceStream.Current).ConfigureAwait(false);
                }

                return hasResult;
            }

            private async ValueTask<IReadOnlyQueryResult> ExecuteQueryAsync(object message)
            {
                IExecutionContext context = _contextFactory(message);
                return await _executeQuery(context, _cancellationToken).ConfigureAwait(false);
            }

            public async ValueTask DisposeAsync()
            {
                if (!_disposed)
                {
                    await _sourceStream.DisposeAsync();
                    _serviceScope.Dispose();
                    _disposed = true;
                }
            }
        }
    }
}
