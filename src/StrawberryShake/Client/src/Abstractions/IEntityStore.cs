using System;
using System.Collections.Generic;

namespace StrawberryShake
{
    public interface IEntityStore : IObservable<ISet<EntityId>>
    {
        T GetOrCreate<T>(EntityId id) where T : class;

        T? GetEntity<T>(EntityId id) where T : class;
        IReadOnlyList<T?> GetEntities<T>(EntityId id) where T : class;

        IDisposable BeginUpdate();
    }
}
