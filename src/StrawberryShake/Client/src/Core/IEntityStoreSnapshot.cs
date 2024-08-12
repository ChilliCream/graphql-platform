using System.Diagnostics.CodeAnalysis;

namespace StrawberryShake;

/// <summary>
/// A store snapshot represents the state of the store at a specific point in time.
/// </summary>
public interface IEntityStoreSnapshot
{
    /// <summary>
    /// Gets an <see cref="ulong"/> representing the version of this snapshot.
    /// </summary>
    ulong Version { get; }

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

    /// <summary>
    /// Tries to get an entity object that is tracked by the store.
    /// </summary>
    /// <param name="id">
    /// The entity id.
    /// </param>
    /// <param name="entity">
    /// The retrieved entity or <c>null</c>.
    /// </param>
    /// <typeparam name="TEntity">
    /// The entity type.
    /// </typeparam>
    /// <returns>
    /// <c>true</c> if an entity was found that matches the <paramref name="id"/> and type.
    /// </returns>
    bool TryGetEntity<TEntity>(EntityId id, [NotNullWhen(true)] out TEntity? entity)
        where TEntity : class;

    /// <summary>
    /// Get entity object which are tracked by this store.
    /// </summary>
    /// <param name="ids">
    /// Entity ids.
    /// </param>
    /// <typeparam name="TEntity">
    /// The entity type.
    /// </typeparam>
    /// <returns></returns>
    IReadOnlyList<TEntity> GetEntities<TEntity>(IEnumerable<EntityId> ids)
        where TEntity : class;

    /// <summary>
    /// Gets all the entities or the entities of a specific type.
    /// </summary>
    IEnumerable<EntityInfo> GetEntities(string? typeName = null);

    /// <summary>
    /// Gets all the entity ids that are currently managed by the store.
    /// </summary>
    IReadOnlyCollection<EntityId> GetEntityIds();
}
