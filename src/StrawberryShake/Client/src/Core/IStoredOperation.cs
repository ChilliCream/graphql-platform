namespace StrawberryShake;

/// <summary>
/// A non generic marker interface for the operation store implementation.
/// </summary>
internal interface IStoredOperation : IDisposable
{
    /// <summary>
    /// Gets the operation request.
    /// </summary>
    public OperationRequest Request { get; }

    /// <summary>
    /// Gets the last result.
    /// </summary>
    public IOperationResult? LastResult { get; }

    /// <summary>
    /// Gets the entities that were used to create this result.
    /// </summary>
    IReadOnlyCollection<EntityId> EntityIds { get; }

    /// <summary>
    /// Gets the current entity store version of this operation.
    /// </summary>
    ulong Version { get; }

    /// <summary>
    /// Gets the count of subscribers that are listening to this operation.
    /// </summary>
    int Subscribers { get; }

    /// <summary>
    /// Gets the time when this operation was last modified.
    /// </summary>
    DateTime LastModified { get; }

    /// <summary>
    /// Clears the currently cached result.
    /// </summary>
    void ClearResult();

    /// <summary>
    /// This will trigger the stored operation to rebuild the result from the entity store.
    /// </summary>
    void UpdateResult(ulong version);

    /// <summary>
    /// This will complete all subscribers.
    /// </summary>
    void Complete();
}
