using System;
using System.Collections.Generic;

namespace StrawberryShake
{
    public interface IEntityStore
    {
        TEntity GetOrCreate<TEntity>(EntityId id)
            where TEntity : class, new();

        TEntity? GetEntity<TEntity>(EntityId id)
            where TEntity : class;

        IReadOnlyList<TEntity> GetEntities<TEntity>(IEnumerable<EntityId> ids)
            where TEntity : class;

        IEntityUpdateSession BeginUpdate();

        IObservable<EntityUpdate> Watch();
    }

    public interface IEntityUpdateSession : IDisposable
    {
        /// <summary>
        /// Gets the store version.
        /// </summary>
        public ulong Version { get; }
    }

    public sealed class EntityUpdate
    {
        public EntityUpdate(ISet<EntityId> updatedEntityIds, ulong version)
        {
            UpdatedEntityIds = updatedEntityIds ??
                throw new ArgumentNullException(nameof(updatedEntityIds));
            Version = version;
        }

        /// <summary>
        /// Gets the ids of the updated entities.
        /// </summary>
        public ISet<EntityId> UpdatedEntityIds { get; }

        /// <summary>
        /// Gets the store version.
        /// </summary>
        public ulong Version { get; }
    }
}
