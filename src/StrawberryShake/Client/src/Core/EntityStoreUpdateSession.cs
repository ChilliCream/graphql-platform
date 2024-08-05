using StrawberryShake.Properties;

namespace StrawberryShake;

internal sealed class EntityStoreUpdateSession : IEntityStoreUpdateSession
{
    private readonly Dictionary<EntityId, object> _entities;

    public EntityStoreUpdateSession(EntityStoreSnapshot originalSnapshot)
    {
        if (originalSnapshot == null)
        {
            throw new ArgumentNullException(nameof(originalSnapshot));
        }

        var version = originalSnapshot.Version;

        unchecked
        {
            version++;
        }

        _entities = originalSnapshot.Copy();
        CurrentSnapshot = new EntityStoreSnapshot(_entities, version);
    }

    public EntityStoreSnapshot CurrentSnapshot { get; }

    public HashSet<EntityId> UpdatedEntityIds { get; } = [];

    IEntityStoreSnapshot IEntityStoreUpdateSession.CurrentSnapshot => CurrentSnapshot;

    public void SetEntity<TEntity>(EntityId id, TEntity entity) where TEntity : class
    {
        if (id == default)
        {
            throw new ArgumentException(Resources.EntityStore_InvalidEntityId, nameof(id));
        }

        _entities[id] = entity ?? throw new ArgumentNullException(nameof(entity));
        UpdatedEntityIds.Add(id);
    }

    public void RemoveEntity(EntityId id)
    {
        if (id == default)
        {
            throw new ArgumentException(Resources.EntityStore_InvalidEntityId, nameof(id));
        }

        _entities.Remove(id);
        UpdatedEntityIds.Add(id);
    }

    public void RemoveEntityRange(IEnumerable<EntityId> entityIds)
    {
        if (entityIds == null)
        {
            throw new ArgumentNullException(nameof(entityIds));
        }

        foreach (var entityId in entityIds)
        {
            _entities.Remove(entityId);
            UpdatedEntityIds.Add(entityId);
        }
    }
}
