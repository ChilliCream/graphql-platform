namespace Mocha.Sagas;

/// <summary>
/// Represents a transaction for saga persistence operations that can be committed or rolled back.
/// </summary>
public interface ISagaTransaction : IAsyncDisposable
{
    /// <summary>
    /// Commits the transaction, persisting all changes made during the transaction scope.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task CommitAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Rolls back the transaction, discarding all changes made during the transaction scope.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task RollbackAsync(CancellationToken cancellationToken);
}
