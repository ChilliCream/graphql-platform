namespace StrawberryShake;

/// <summary>
/// The entity store can be used to access and mutate entities.
/// </summary>
public interface IEntityStore : IDisposable
{
    /// <summary>
    /// Gets the current snapshot of the store.
    /// </summary>
    IEntityStoreSnapshot CurrentSnapshot { get; }

    /// <summary>
    /// Updates the store by modifying entities.
    /// Updating the store will cause a new snapshot.
    /// </summary>
    /// <param name="action">
    /// The action that represents the store mutation.
    /// </param>
    void Update(Action<IEntityStoreUpdateSession> action);

    /// <summary>
    /// Observe the entity store.
    /// </summary>
    IObservable<EntityUpdate> Watch();
}
