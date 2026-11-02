namespace Mocha.Sagas.EfCore;

/// <summary>
/// A saga transaction implementation that performs no operations, used when a database
/// transaction is already active and nesting should be avoided.
/// </summary>
internal sealed class NoOpSagaTransaction : ISagaTransaction
{
    /// <summary>
    /// Completes immediately without performing any commit operation.
    /// </summary>
    /// <param name="cancellationToken">Unused cancellation token.</param>
    /// <returns>A completed task.</returns>
    public Task CommitAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Completes immediately without performing any rollback operation.
    /// </summary>
    /// <param name="cancellationToken">Unused cancellation token.</param>
    /// <returns>A completed task.</returns>
    public Task RollbackAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Completes immediately without releasing any resources.
    /// </summary>
    /// <returns>A default <see cref="ValueTask"/>.</returns>
    public ValueTask DisposeAsync() => default;

    /// <summary>
    /// Gets the shared singleton instance of <see cref="NoOpSagaTransaction"/>.
    /// </summary>
    public static NoOpSagaTransaction Instance { get; } = new();
}
