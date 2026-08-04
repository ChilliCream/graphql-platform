namespace Mocha.Sagas;

/// <summary>
/// Provides persistence operations for saga state, including loading, saving, and deleting saga instances.
/// </summary>
public interface ISagaStore
{
    /// <summary>
    /// Starts a new transaction for saga persistence operations.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A transaction that can be committed or rolled back.</returns>
    Task<ISagaTransaction> StartTransactionAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Saves the saga state to persistent storage.
    /// </summary>
    /// <typeparam name="T">The saga state type.</typeparam>
    /// <param name="saga">The saga definition.</param>
    /// <param name="state">The saga state to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task SaveAsync<T>(Saga saga, T state, CancellationToken cancellationToken) where T : SagaStateBase;

    /// <summary>
    /// Deletes a saga instance from persistent storage.
    /// </summary>
    /// <param name="saga">The saga definition.</param>
    /// <param name="id">The identifier of the saga instance to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task DeleteAsync(Saga saga, Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Loads a saga state from persistent storage, or returns <c>null</c> if not found.
    /// </summary>
    /// <typeparam name="T">The saga state type.</typeparam>
    /// <param name="saga">The saga definition.</param>
    /// <param name="id">The identifier of the saga instance to load.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The loaded saga state, or <c>null</c> if no instance was found.</returns>
    Task<T?> LoadAsync<T>(Saga saga, Guid id, CancellationToken cancellationToken);
}
