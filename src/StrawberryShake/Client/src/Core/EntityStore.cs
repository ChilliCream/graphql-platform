using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace StrawberryShake
{
    /// <summary>
    /// The entity store can be used to access and mutate entities.
    /// </summary>
    public class EntityStore : IEntityStore
    {
        private readonly object _sync = new();
        private readonly EntityUpdateObservable _entityUpdateObservable = new();
        private EntityStoreSnapshot _snapshot = new();

        /// <inheritdoc />
        public IEntityStoreSnapshot CurrentSnapshot => _snapshot;

        /// <inheritdoc />
        public void Update(Action<IEntityStoreUpdateSession> action)
        {
            lock (_sync)
            {
                var session = new EntityStoreUpdateSession(_snapshot);

                action(session);

                _snapshot = session.CurrentSnapshot;
                _entityUpdateObservable.OnUpdated(
                    session.CurrentSnapshot,
                    session.UpdatedEntityIds,
                    session.CurrentSnapshot.Version);
            }
        }

        /// <inheritdoc />
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
                    _observers = _observers.Add(observer);
                }

                return new Subscription(this, observer);
            }

            public void OnUpdated(
                IEntityStoreSnapshot snapshot,
                ISet<EntityId> entityIds,
                ulong version)
            {
                ImmutableList<IObserver<EntityUpdate>> observers = _observers;

                if (observers.Count > 0)
                {
                    var update = new EntityUpdate(snapshot, entityIds, version);

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
    }
}
