using System;
using System.Collections.Generic;

namespace StrawberryShake
{
    /// <summary>
    /// The entity store tracks and stores the GraphQL entities.
    /// </summary>
    public interface IEntityStore
    {
        /// <summary>
        /// Gets or creates an entity object that is tracked by the store.
        /// </summary>
        /// <param name="id">
        /// The entity id.
        /// </param>
        /// <typeparam name="TEntity">
        /// The entity type.
        /// </typeparam>
        /// <returns>
        /// The retrieved or created entity instance.
        /// </returns>
        TEntity GetOrCreate<TEntity>(EntityId id)
            where TEntity : class, new();

        /// <summary>
        /// Gets an entity object that is tracked by the store.
        /// </summary>
        /// <param name="id">
        /// The entity id.
        /// </param>
        /// <typeparam name="TEntity">
        /// The entity type.
        /// </typeparam>
        /// <returns>
        /// The retrieved entity instance.
        /// </returns>
        TEntity? GetEntity<TEntity>(EntityId id)
            where TEntity : class;

        IReadOnlyList<TEntity> GetEntities<TEntity>(IEnumerable<EntityId> ids)
            where TEntity : class;

        /// <summary>
        /// Begins a session that allows to safely update and read from this store.
        /// </summary>
        IEntityUpdateSession BeginUpdate();

        IObservable<EntityUpdate> Watch();
    }
}
