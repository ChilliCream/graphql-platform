using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake
{
    internal sealed class OperationStore
        : IOperationStore
        , IDisposable
    {
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly ConcurrentDictionary<IOperationRequest, IStoredOperation> _results = new();
        private bool _disposed;

        public async ValueTask SetAsync<T>(
            IOperationRequest operationRequest,
            IOperationResult<T> operationResult,
            CancellationToken cancellationToken = default)
            where T : class
        {
            if (operationRequest == null)
            {
                throw new ArgumentNullException(nameof(operationRequest));
            }

            if (operationResult == null)
            {
                throw new ArgumentNullException(nameof(operationResult));
            }

            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(OperationStore));
            }

            StoredOperation<T> storedOperation = GetOrAddStoredOperation<T>(operationRequest);

            await _semaphore
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            try
            {
                await storedOperation
                    .SetResultAsync(operationResult, cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public bool TryGet<T>(
            IOperationRequest operationRequest,
            [NotNullWhen(true)] out IOperationResult<T>? result)
            where T : class
        {
            if (operationRequest == null)
            {
                throw new ArgumentNullException(nameof(operationRequest));
            }

            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(OperationStore));
            }

            if (_results.TryGetValue(operationRequest, out var storedOperation) &&
                storedOperation is StoredOperation<T> { LastResult: not null } casted)
            {
                result = casted.LastResult;
                return true;
            }

            result = null;
            return false;
        }

        public IOperationObservable<T> Watch<T>(
            IOperationRequest operationRequest)
            where T : class
        {
            if (operationRequest == null)
            {
                throw new ArgumentNullException(nameof(operationRequest));
            }

            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(OperationStore));
            }

            return GetOrAddStoredOperation<T>(operationRequest);
        }

        private StoredOperation<T> GetOrAddStoredOperation<T>(
            IOperationRequest request)
            where T : class
        {
            if(_results.GetOrAdd(request, k => new StoredOperation<T>(k)) is StoredOperation<T> t)
            {
                return t;
            }

            // this should never occur.
            throw new InvalidOperationException();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _semaphore.Dispose();
                _disposed = true;
            }
        }
    }
}
