using System;
using System.Collections.Generic;
using System.Threading;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Batching
{
    public class BatchQueryExecutionResult
        : IBatchQueryExecutionResult
    {
        private static readonly IReadOnlyDictionary<string, object> _empty =
            new Dictionary<string, object>();

        private readonly IQueryExecutor _executor;
        private readonly IErrorHandler _errorHandler;
        private readonly ITypeConversion _typeConversion;
        private readonly IReadOnlyList<IReadOnlyQueryRequest> _batch;
        private BatchQueryExecutionEnumerator _enumerator;

        public BatchQueryExecutionResult(
            IQueryExecutor executor,
            IErrorHandler errorHandler,
            ITypeConversion typeConversion,
            IReadOnlyList<IReadOnlyQueryRequest> batch)
        {
            _executor = executor
                ?? throw new ArgumentNullException(nameof(executor));
            _errorHandler = errorHandler
                ?? throw new ArgumentNullException(nameof(errorHandler));
            _typeConversion = typeConversion
                ?? throw new ArgumentNullException(nameof(typeConversion));
            _batch = batch
                ?? throw new ArgumentNullException(nameof(batch));
        }

        public IReadOnlyCollection<IError> Errors => Array.Empty<IError>();

        public IReadOnlyDictionary<string, object> Extensions => _empty;

        public IReadOnlyDictionary<string, object> ContextData => _empty;

        public IAsyncEnumerator<IReadOnlyQueryResult> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
        {
            if (_enumerator is null)
            {
                _enumerator = new BatchQueryExecutionEnumerator(
                    _executor,
                    _errorHandler,
                    _typeConversion,
                    _batch,
                    cancellationToken);
            }

            if (_enumerator.IsCompleted)
            {
                throw new InvalidOperationException(
                    "The stream has been completed and cannot be replayed.");
            }

            return _enumerator;
        }
    }
}
