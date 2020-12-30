using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace StrawberryShake.Impl
{
    internal sealed class OperationStore
        : IOperationStore
        , IDisposable
    {
        private readonly ConcurrentDictionary<OperationRequest, IStoredOperation> _results = new();
        private readonly IDisposable _entityChangeObserverSession;
        private bool _disposed;

        public OperationStore(IObservable<ISet<EntityId>> entityChangeObserver)
        {
            _entityChangeObserverSession = entityChangeObserver.Subscribe(OnEntityUpdate);
        }

        public void Set<T>(
            OperationRequest operationRequest,
            IOperationResult<T> operationResult)
            where T : class
        {
            if (operationRequest is null)
            {
                throw new ArgumentNullException(nameof(operationRequest));
            }

            if (operationResult is null)
            {
                throw new ArgumentNullException(nameof(operationResult));
            }

            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(OperationStore));
            }

            StoredOperation<T> storedOperation = GetOrAddStoredOperation<T>(operationRequest);
            storedOperation.SetResult(operationResult);
        }

        public bool TryGet<T>(
            OperationRequest operationRequest,
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

        public IObservable<IOperationResult<T>> Watch<T>(
            OperationRequest operationRequest)
            where T : class
        {
            if (operationRequest is null)
            {
                throw new ArgumentNullException(nameof(operationRequest));
            }

            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(OperationStore));
            }

            return GetOrAddStoredOperation<T>(operationRequest);
        }

        private void OnEntityUpdate(ISet<EntityId> updatedEntities)
        {
            if (_disposed)
            {
                return;
            }

            foreach (IStoredOperation operation in _results.Values)
            {
                if (updatedEntities.Overlaps(operation.EntityIds))
                {
                    operation.UpdateResult();
                }
            }
        }

        private StoredOperation<T> GetOrAddStoredOperation<T>(
            OperationRequest request)
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
                _entityChangeObserverSession.Dispose();
                _disposed = true;
            }
        }
    }
}
