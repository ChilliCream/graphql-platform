using System;
using System.Threading;
using System.Threading.Channels;

namespace StrawberryShake
{
    /// <summary>
    /// The entity store can be used to access and mutate entities.
    /// </summary>
    public partial class EntityStore : IEntityStore
    {
        private readonly object _sync = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly Channel<EntityUpdate> _updates = Channel.CreateUnbounded<EntityUpdate>();
        private readonly EntityUpdateObservable _entityUpdateObservable = new();
        private EntityStoreSnapshot _snapshot = new();
        private bool _disposed;

        public EntityStore()
        {
            BeginProcessEntityUpdates();
        }

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
                _updates.Writer.TryWrite(new EntityUpdate(
                    session.CurrentSnapshot,
                    session.UpdatedEntityIds));
            }
        }

        /// <inheritdoc />
        public IObservable<EntityUpdate> Watch() => _entityUpdateObservable;

        public void Dispose()
        {
            if (!_disposed)
            {
                _cts.Cancel();
                _cts.Dispose();
                _disposed = true;
            }
        }
    }
}
