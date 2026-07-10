namespace Mocha.Sagas;

/// <summary>
/// A no-operation saga transaction used when a transaction is already in progress.
/// </summary>
internal sealed class NoOpSagaTransaction : ISagaTransaction
{
    public static NoOpSagaTransaction Instance { get; } = new();

    private NoOpSagaTransaction() { }

    public Task CommitAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task RollbackAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
