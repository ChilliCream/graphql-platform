using System;
using System.Collections.Generic;

namespace StrawberryShake
{
    public interface IEntityStore : IObservable<ISet<EntityId>>
    {
        TEntity GetOrCreate<TEntity>(EntityId id) where TEntity : class;

        TEntity? GetEntity<TEntity>(EntityId id) where TEntity : class;

        IReadOnlyList<TEntity?> GetEntities<TEntity>(IEnumerable<EntityId> id)
            where TEntity : class;

        IDisposable BeginUpdate();

        IDisposable BeginRead();
    }
}
