using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net;
using System.Threading;

namespace StrawberryShake.Impl
{
    public class EntityStore : IEntityStore
    {
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly EntityUpdateObservable _entityUpdateObservable = new();
        private readonly ConcurrentDictionary<EntityId, object> _entities = new();
        private ulong _version;
        private UpdateSession? _currentUpdateSession;

        public TEntity GetOrCreate<TEntity>(EntityId id)
            where TEntity : class, new()
        {
            if (id == default)
            {
                throw new ArgumentException("Invalid entity id.", nameof(id));
            }

            UpdateSession? session = _currentUpdateSession;

            if (session is null)
            {
                throw new InvalidOperationException(
                    "You need to first acquire an update session.");
            }

            session.EntityIds.Add(id);

            if (_entities.GetOrAdd(id, k => new TEntity()) is TEntity entity)
            {
                return entity;
            }

            throw new InvalidOperationException(
                "The entity type does not match the stored entity.");
        }

        public TEntity? GetEntity<TEntity>(EntityId id)
            where TEntity : class
        {
            if (id == default)
            {
                throw new ArgumentException("Invalid entity id.", nameof(id));
            }

            if (_currentUpdateSession is null)
            {
                throw new InvalidOperationException(
                    "You need to first acquire an update session.");
            }

            if (_entities.TryGetValue(id, out object? o) && o is TEntity entity)
            {
                return entity;
            }

            return default;
        }

        public IReadOnlyList<TEntity> GetEntities<TEntity>(IEnumerable<EntityId> ids)
            where TEntity : class
        {
            if (ids is null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            if (_currentUpdateSession is null)
            {
                throw new InvalidOperationException(
                    "You need to first acquire a update session.");
            }

            var entities = new List<TEntity>();

            foreach (EntityId id in ids)
            {
                if (id != default &&
                    _entities.TryGetValue(id, out object? o) &&
                    o is TEntity entity)
                {
                    entities.Add(entity);
                }
            }

            return entities;
        }

        public IEntityUpdateSession BeginUpdate()
        {
            _semaphore.Wait();

            return _currentUpdateSession = new UpdateSession(++_version, FinalizeUpdate);

            void FinalizeUpdate(ulong version, ISet<EntityId> updatedEntityIds)
            {
                _currentUpdateSession = null;
                _entityUpdateObservable.OnUpdated(updatedEntityIds, version);
                _semaphore.Release();
            }
        }

        public IObservable<EntityUpdate> Watch() => _entityUpdateObservable;

        private class EntityUpdateObservable : IObservable<EntityUpdate>
        {
            private readonly object _sync = new();
            private ImmutableList<IObserver<EntityUpdate>> _observers =
                ImmutableList<IObserver<EntityUpdate>>.Empty;

            public IDisposable Subscribe(IObserver<EntityUpdate> observer)
            {
                lock (_sync)
                {
                    _observers = _observers.Remove(observer);
                }

                return new Subscription(this, observer);
            }

            public void OnUpdated(ISet<EntityId> entityIds, ulong version)
            {
                ImmutableList<IObserver<EntityUpdate>> observers = _observers;

                if (observers.Count > 0)
                {
                    var update = new EntityUpdate(entityIds, version);

                    foreach (var observer in observers)
                    {
                        observer.OnNext(update);
                    }
                }
            }

            private class Subscription : IDisposable
            {
                private readonly EntityUpdateObservable _observable;
                private readonly IObserver<EntityUpdate> _observer;
                private bool _disposed;

                public Subscription(
                    EntityUpdateObservable observable,
                    IObserver<EntityUpdate> observer)
                {
                    _observable = observable;
                    _observer = observer;
                }

                public void Dispose()
                {
                    if (!_disposed)
                    {
                        lock (_observable._sync)
                        {
                            _observable._observers = _observable._observers.Remove(_observer);
                        }
                        _disposed = true;
                    }
                }
            }
        }

        private class UpdateSession : IEntityUpdateSession
        {
            private readonly ulong _version;
            private readonly Action<ulong, ISet<EntityId>> _dispose;
            private bool _disposed;

            public UpdateSession(ulong version, Action<ulong, ISet<EntityId>> dispose)
            {
                _version = version;
                _dispose = dispose;
            }

            public ulong Version => _version;

            public HashSet<EntityId> EntityIds { get; } = new();

            public void Dispose()
            {
                if (!_disposed)
                {
                    _dispose(_version, EntityIds);
                    _disposed = true;
                }
            }
        }
    }
}
