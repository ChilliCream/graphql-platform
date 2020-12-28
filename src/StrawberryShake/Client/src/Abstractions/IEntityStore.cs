using System;
using System.Collections.Generic;

namespace StrawberryShake
{
    public interface IEntityStore : IObservable<ISet<EntityId>>
    {
        TEntity GetOrCreate<TEntity>(EntityId id) where TEntity : class;

        TEntity? GetEntity<TEntity>(EntityId id) where TEntity : class;

        IReadOnlyList<TEntity?> GetEntities<TEntity>(IEnumerable<EntityId> id) where TEntity : class;

        IDisposable BeginUpdate();

        IDisposable BeginRead();
    }

    public class Foo
    {
        private readonly IOperationStore _operationStore;
        private readonly IEntityStore _entityStore;

        private void UpdateStore()
        {

        }


    }

    public class EntityUpdateObserver : IObserver<ISet<EntityId>>
    {
        private readonly IOperationStore _operationStore;
        private readonly IEntityStore _entityStore;

        public void OnNext(ISet<EntityId> value)
        {
            foreach (var VARIABLE in value.Overlaps(_operationStore.))
            {

            }


            using (_entityStore.BeginRead())
            {
                _entityStore.GetEntities<object>(value)
            }
        }

        public void OnError(Exception error)
        {
        }

        public void OnCompleted()
        {
        }
    }
}
