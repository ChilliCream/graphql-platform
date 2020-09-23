using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Properties;

#nullable enable

namespace HotChocolate.Execution.Processing
{
    public sealed class DeferredResult
        : IExecutionResult
        , IResponseStream
    {
        private readonly IQueryResult _initialResult;
        private readonly IAsyncEnumerable<IQueryResult> _deferredResults;
        private readonly IDisposable? _session;
        private bool _isRead;
        private bool _disposed;

        public DeferredResult(
            IQueryResult initialResult,
            IAsyncEnumerable<IQueryResult> deferredResults,
            IReadOnlyDictionary<string, object?>? extensions = null,
            IReadOnlyDictionary<string, object?>? contextData = null,
            IDisposable? session = null)
        {
            _initialResult = initialResult ??
                throw new ArgumentNullException(nameof(initialResult));
            _deferredResults = deferredResults ??
                throw new ArgumentNullException(nameof(deferredResults));
            Extensions = extensions;
            ContextData = contextData;
            _session = session;
        }

        public IReadOnlyList<IError>? Errors => null;

        public IReadOnlyDictionary<string, object?>? Extensions { get; }

        public IReadOnlyDictionary<string, object?>? ContextData { get; }

        public IAsyncEnumerable<IQueryResult> ReadResultsAsync()
        {
            if (_isRead)
            {
                throw new InvalidOperationException(
                    Resources.DeferredResult_ReadResultsAsync_ReadOnlyOnce);
            }

            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SubscriptionResult));
            }

            _isRead = true;
            return new EnumerateResults(_initialResult, _deferredResults, this);
        }

        public ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _session?.Dispose();
                _disposed = true;
            }
            return default;
        }

        private class EnumerateResults : IAsyncEnumerable<IQueryResult>
        {
            private readonly IQueryResult _initialResult;
            private readonly IAsyncEnumerable<IQueryResult> _deferredResults;
            private readonly IAsyncDisposable _session;

            public EnumerateResults(
                IQueryResult initialResult,
                IAsyncEnumerable<IQueryResult> deferredResults,
                IAsyncDisposable session)
            {
                _initialResult = initialResult;
                _deferredResults = deferredResults;
                _session = session;
            }

            public async IAsyncEnumerator<IQueryResult> GetAsyncEnumerator(
                CancellationToken cancellationToken)
            {
                yield return _initialResult;

                await foreach (IQueryResult deferredResult in
                    _deferredResults.WithCancellation(cancellationToken))
                {
                    yield return deferredResult;
                }

                await _session.DisposeAsync();
            }
        }
    }
}
