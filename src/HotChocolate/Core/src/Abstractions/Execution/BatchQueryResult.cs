using System;
using System.Collections.Generic;
using System.Threading.Tasks;

#nullable enable

namespace HotChocolate.Execution
{
    public class BatchQueryResult : IBatchQueryResult
    {
        private readonly Func<IAsyncEnumerable<IQueryResult>>? _resultStreamFactory;
        private readonly IAsyncDisposable? _session;
        private bool _isRead;
        private bool _disposed;

        public BatchQueryResult(
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
            Errors = errors;
            Extensions = extensions;
            ContextData = contextData;
            _session = session;
        }

        public IReadOnlyList<IError>? Errors { get; }

        public IReadOnlyDictionary<string, object?>? Extensions { get; }

        public IReadOnlyDictionary<string, object?>? ContextData { get; }

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
                if (_session is { })
                {
                    await _session.DisposeAsync().ConfigureAwait(false);
                }
                _disposed = true;
            }
        }
    }
}
