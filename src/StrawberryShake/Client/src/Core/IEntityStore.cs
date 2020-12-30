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
}
