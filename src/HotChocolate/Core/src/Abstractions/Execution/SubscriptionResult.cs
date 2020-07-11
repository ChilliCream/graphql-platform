using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        private readonly IAsyncDisposable? _subscription;
        private bool _isRead = false;
        private bool _disposed = false;

        public SubscriptionResult(
            Func<IAsyncEnumerable<IQueryResult>>? resultStreamFactory,
            IReadOnlyList<IError>? errors,
            IReadOnlyDictionary<string, object?>? extensions = null,
            IReadOnlyDictionary<string, object?>? contextData = null,
            IAsyncDisposable? subscription = null)
        {
            if (resultStreamFactory is null && errors is null)
            {
                throw new ArgumentException("Either provide a result stream factory or errors.");
            }

            _resultStreamFactory = resultStreamFactory;
            _errors = errors;
            _extensions = extensions;
            _contextData = contextData;
            _subscription = subscription;
        }

        public IReadOnlyList<IError>? Errors => _errors;

        public IReadOnlyDictionary<string, object?>? Extensions => _extensions;

        public IReadOnlyDictionary<string, object?>? ContextData => _contextData;

        public IAsyncEnumerable<IQueryResult> ReadResultsAsync()
        {
            if (_resultStreamFactory is null)
            {
                // todo : throw helper
                throw new InvalidOperationException(
                    "This result has errors and cannot read from the response stream.");
            }

            if (_isRead)
            {
                // todo : throw helper
                throw new InvalidOperationException(
                    "You can only read a response stream once.");
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
                if (_subscription is { })
                {
                    await _subscription.DisposeAsync().ConfigureAwait(false);
                }
                _disposed = true;
            }
        }

    }
}
