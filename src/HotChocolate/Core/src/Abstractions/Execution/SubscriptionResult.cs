using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Execution
{
    public sealed class SubscriptionResult
        : ISubscriptionResult
    {
        private readonly Func<IAsyncEnumerable<IQueryResult>>? _resultStreamFactory;
        private readonly IReadOnlyList<IError>? _errors;
        private readonly IReadOnlyDictionary<string, object?>? _extensions;
        private readonly IReadOnlyDictionary<string, object?>? _contextData;
        private readonly IAsyncDisposable? _session;
        private bool _isRead;
        private bool _disposed;

        public SubscriptionResult(
            Func<IAsyncEnumerable<IQueryResult>>? resultStreamFactory,
            IReadOnlyList<IError>? errors,
            IReadOnlyDictionary<string, object?>? extensions = null,
            IReadOnlyDictionary<string, object?>? contextData = null,
            IAsyncDisposable? session = null)
        {
            if (resultStreamFactory is null && errors is null)
            {
                throw new ArgumentException("Either provide a result stream factory or errors.");
            }

            _resultStreamFactory = resultStreamFactory;
            _errors = errors;
            _extensions = extensions;
            _contextData = contextData;
            _session = session;
        }

        public SubscriptionResult(
            SubscriptionResult subscriptionResult,
            IAsyncDisposable? session = null)
        {
            _resultStreamFactory = subscriptionResult._resultStreamFactory;
            _errors = subscriptionResult._errors;
            _extensions = subscriptionResult._extensions;
            _contextData = subscriptionResult._contextData;
            _session = session is null
                ? (IAsyncDisposable)subscriptionResult
                : new CombinedDispose(session, subscriptionResult);
        }

         public SubscriptionResult(
            SubscriptionResult subscriptionResult,
            IDisposable? session = null)
        {
            _resultStreamFactory = subscriptionResult._resultStreamFactory;
            _errors = subscriptionResult._errors;
            _extensions = subscriptionResult._extensions;
            _contextData = subscriptionResult._contextData;
            _session = session is null
                ? (IAsyncDisposable)subscriptionResult
                : new CombinedDispose(session.Dispose, subscriptionResult);
        }

        public IReadOnlyList<IError>? Errors => _errors;

        public IReadOnlyDictionary<string, object?>? Extensions => _extensions;

        public IReadOnlyDictionary<string, object?>? ContextData => _contextData;

        public IAsyncEnumerable<IQueryResult> ReadResultsAsync()
        {
            if (_resultStreamFactory is null)
            {
                throw new InvalidOperationException(
                    AbstractionResources.SubscriptionResult_ResultHasErrors);
            }

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
            return _resultStreamFactory();
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                if (_session is { })
                {
                    await _session.DisposeAsync().ConfigureAwait(false);
                }
                _disposed = true;
            }
        }

        private class CombinedDispose : IAsyncDisposable
        {
            private readonly IAsyncDisposable? _aa;
            private readonly Action? _ab;
            private readonly IAsyncDisposable _b;
            private bool _disposed;

            public CombinedDispose(IAsyncDisposable a, IAsyncDisposable b)
            {
                _aa = a;
                _b = b;
            }

            public CombinedDispose(Action a, IAsyncDisposable b)
            {
                _ab = a;
                _b = b;
            }

            public async ValueTask DisposeAsync()
            {
                if (!_disposed)
                {
                    if (_aa is not null)
                    {
                        await _aa.DisposeAsync().ConfigureAwait(false);
                    }

                    _ab?.Invoke();

                    await _b.DisposeAsync().ConfigureAwait(false);
                    _disposed = true;
                }
            }
        }
    }
}
