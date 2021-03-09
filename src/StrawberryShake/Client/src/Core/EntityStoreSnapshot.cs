using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace StrawberryShake
{
    internal sealed class EntityStoreSnapshot : IEntityStoreSnapshot
    {
        private readonly Dictionary<EntityId, object> _entities;

        public EntityStoreSnapshot()
        {
            _entities = new();
        }

        public EntityStoreSnapshot(Dictionary<EntityId, object> entities, ulong version)
        {
            _entities = entities;
            Version = version;
        }

        internal Dictionary<EntityId, object> Copy()
        {
            return new(_entities);
        }

        public ulong Version { get; } = 0;

        public TEntity? GetEntity<TEntity>(EntityId id)
            where TEntity : class
        {
            if (_entities.TryGetValue(id, out var value) && value is TEntity entity)
            {
                return entity;
            }

            return null;
        }

        public bool TryGetEntity<TEntity>(EntityId id, [NotNullWhen(true)] out TEntity? entity)
            where TEntity : class
        {
            if (_entities.TryGetValue(id, out var value) && value is TEntity casted)
            {
                entity = casted;
                return true;
            }

            entity = null;
            return false;
        }

        public IReadOnlyList<TEntity> GetEntities<TEntity>(IEnumerable<EntityId> ids)
            where TEntity : class
        {
            var list = new List<TEntity>();

            foreach (EntityId id in ids)
            {
                if (TryGetEntity(id, out TEntity? entity))
                {
                    list.Add(entity);
                }
            }

            return list;
        }

        public IReadOnlyCollection<EntityId> GetEntityIds() =>
            _entities.Keys;
    }
}
