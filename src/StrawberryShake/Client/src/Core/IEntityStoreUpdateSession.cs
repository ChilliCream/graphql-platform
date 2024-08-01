namespace StrawberryShake;

/// <summary>
/// The update session can be used to create a new store snapshot that will replace the
/// current one.
/// </summary>
public interface IEntityStoreUpdateSession
{
    /// <summary>
    /// Gets the snapshot that is being created by this store update.
    /// </summary>
    IEntityStoreSnapshot CurrentSnapshot { get; }

    /// <summary>
    /// Adds or replaces an entity.
    /// </summary>
    /// <param name="id">The entity id.</param>
    /// <param name="entity">The entity.</param>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    void SetEntity<TEntity>(EntityId id, TEntity entity) where TEntity : class;

    /// <summary>
    /// Removes an entity from the store.
    /// </summary>
    /// <param name="id">The entity id.</param>
    void RemoveEntity(EntityId id);

    /// <summary>
    /// Removes a range of entities from the store.
    /// </summary>
    /// <param name="entityIds">The entity ids.</param>
    void RemoveEntityRange(IEnumerable<EntityId> entityIds);
}
