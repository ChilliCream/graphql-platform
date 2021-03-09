using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace StrawberryShake
{
    public sealed class OperationStore
        : IOperationStore
        , IDisposable
    {
        private readonly ConcurrentDictionary<OperationRequest, IStoredOperation> _results = new();
        private readonly IEntityStore _entityStore;
        private readonly OperationStoreObservable _operationStoreObservable = new();
        private readonly IDisposable _entityChangeObserverSession;
        private bool _disposed;

        public OperationStore(IEntityStore entityStore)
        {
            _entityStore = entityStore ?? throw new ArgumentNullException(nameof(entityStore));
            _entityChangeObserverSession = _entityStore.Watch().Subscribe(OnEntityUpdate);
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

        public void Reset(OperationRequest operationRequest)
        {
            if (operationRequest is null)
            {
                throw new ArgumentNullException(nameof(operationRequest));
            }

            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(OperationStore));
            }

            if (_results.TryGetValue(operationRequest, out var storedOperation))
            {
                storedOperation.ClearResult();
            }
        }

        public void Remove(OperationRequest operationRequest)
        {
            if (operationRequest == null)
            {
                throw new ArgumentNullException(nameof(operationRequest));
            }

            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(OperationStore));
            }

            if (_results.TryRemove(operationRequest, out var storedOperation))
            {
                storedOperation.Complete();

                _entityStore.Update(session =>
                {
                    session.RemoveEntityRange(
                        _entityStore.CurrentSnapshot.GetEntityIds().Except(
                            _results.Values.SelectMany(t => t.EntityIds)));
                });
            }
        }

        public void Clear()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(OperationStore));
            }

            ICollection<IStoredOperation> results = _results.Values;
            _results.Clear();

            foreach (var result in results)
            {
                result.Complete();
            }

            _entityStore.Update(session =>
            {
                session.RemoveEntityRange(
                    _entityStore.CurrentSnapshot.GetEntityIds().Except(
                        _results.Values.SelectMany(t => t.EntityIds)));
            });
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
                result = casted.LastResult!;
                return true;
            }

            result = null;
            return false;
        }

        public IEnumerable<StoredOperationVersion> GetAll()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(OperationStore));
            }

            return _results.Values.Select(
                op => new StoredOperationVersion(
                    op.Request,
                    op.LastResult,
                    op.Version,
                    op.Subscribers,
                    op.LastModified));
        }

        public IReadOnlyList<EntityId> GetUsedEntityIds()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(OperationStore));
            }

            return _results.Values.SelectMany(t => t.EntityIds).ToArray();
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

        public IObservable<OperationUpdate> Watch()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(OperationStore));
            }

            return _operationStoreObservable;
        }

        private void OnEntityUpdate(EntityUpdate update)
        {
            if (_disposed)
            {
                return;
            }

            var updated = new List<StoredOperationVersion>();

            foreach (IStoredOperation operation in _results.Values)
            {
                if (operation.Version < update.Version &&
                    update.UpdatedEntityIds.Overlaps(operation.EntityIds))
                {
                    operation.UpdateResult(update.Version);
                    updated.Add(new(
                        operation.Request,
                        operation.LastResult,
                        operation.Version,
                        operation.Subscribers,
                        operation.LastModified));
                }
            }

            if (updated.Count > 0)
            {
                // The observables will run in the current edit session
                _operationStoreObservable.Next(
                    new OperationUpdate(OperationUpdateKind.Updated, updated));
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

        private class OperationStoreObservable
            : IObservable<OperationUpdate>
            , IDisposable
        {
            private readonly object _sync = new();
            private ImmutableList<OperationStoreSession> _sessions =
                ImmutableList<OperationStoreSession>.Empty;

            public void Next(OperationUpdate operationUpdate)
            {
                ImmutableList<OperationStoreSession> sessions = _sessions;

                foreach (var session in sessions)
                {
                    session.Next(operationUpdate);
                }
            }

            public void Complete()
            {
                ImmutableList<OperationStoreSession> sessions = _sessions;

                foreach (var session in sessions)
                {
                    session.Complete();
                }
            }

            public IDisposable Subscribe(IObserver<OperationUpdate> observer)
            {
                var session = new OperationStoreSession(observer, Unsubscribe);

                lock (_sync)
                {
                    _sessions = _sessions.Add(session);
                }

                return session;
            }

            private void Unsubscribe(OperationStoreSession session)
            {
                lock (_sync)
                {
                    _sessions = _sessions.Remove(session);
                }
            }

            public void Dispose()
            {
                ImmutableList<OperationStoreSession> sessions = _sessions;

                foreach (var session in sessions)
                {
                    session.Complete();
                    session.Dispose();
                }
            }

            private class OperationStoreSession
                : IDisposable
            {
                private readonly IObserver<OperationUpdate> _observer;
                private readonly Action<OperationStoreSession> _unsubscribe;

                public OperationStoreSession(
                    IObserver<OperationUpdate> observer,
                    Action<OperationStoreSession> unsubscribe)
                {
                    _observer = observer;
                    _unsubscribe = unsubscribe;
                }

                public void Next(OperationUpdate operationUpdate)
                {
                    _observer.OnNext(operationUpdate);
                }

                public void Complete()
                {
                    _observer.OnCompleted();
                }

                public void Dispose()
                {
                    _unsubscribe(this);
                }
            }
        }
    }
}
