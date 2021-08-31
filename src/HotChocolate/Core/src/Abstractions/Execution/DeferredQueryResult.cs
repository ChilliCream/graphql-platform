using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Execution
{
    public sealed class DeferredQueryResult
        : IExecutionResult
        , IResponseStream
    {
        private readonly IQueryResult _initialResult;
        private readonly IAsyncEnumerable<IQueryResult> _deferredResults;
        private IDisposable? _session;
        private bool _isRead;
        private bool _disposed;

        public DeferredQueryResult(
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

        public DeferredQueryResult(DeferredQueryResult queryResult, IDisposable session)
        {
            if (queryResult is null)
            {
                throw new ArgumentNullException(nameof(queryResult));
            }

            if (session is null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            _initialResult = queryResult._initialResult;
            _deferredResults = queryResult._deferredResults;
            Extensions = queryResult.Extensions;
            ContextData = queryResult.ContextData;
            _session = queryResult._session is not null
                ? new CombinedDispose(queryResult._session, session)
                : session;
        }

        public IReadOnlyList<IError>? Errors => null;

        public IReadOnlyDictionary<string, object?>? Extensions { get; }

        public IReadOnlyDictionary<string, object?>? ContextData { get; }

        public IAsyncEnumerable<IQueryResult> ReadResultsAsync()
        {
            if (_isRead)
            {
                throw new InvalidOperationException(
                    AbstractionResources.SubscriptionResult_ReadOnlyOnce);
            }

            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SubscriptionResult));
            }

            _isRead = true;
            return new EnumerateResults(_initialResult, _deferredResults, this);
        }

        /// <inheritdoc />
        public void RegisterDisposable(IDisposable disposable)
        {
            if (disposable is null)
            {
                throw new ArgumentNullException(nameof(disposable));
            }

            _session = _session.Combine(disposable);
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return default;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _session?.Dispose();
                _disposed = true;
            }
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
                CancellationToken cancellationToken = default)
            {
                yield return _initialResult;

                await foreach (IQueryResult deferredResult in
                    _deferredResults.WithCancellation(cancellationToken))
                {
                    yield return deferredResult;
                }

                await _session.DisposeAsync().ConfigureAwait(false);
            }
        }

        internal class CombinedDispose : IDisposable
        {
            private readonly IDisposable _a;
            private readonly IDisposable _b;
            private bool _disposed;

            public CombinedDispose(IDisposable a, IDisposable b)
            {
                _a = a;
                _b = b;
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _a.Dispose();
                    _b.Dispose();
                    _disposed = true;
                }
            }
        }
    }
}
