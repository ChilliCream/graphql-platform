namespace Mocha.Sagas.Tests;

public class TestSagaTransaction(Func<ValueTask>? onTransactionComplete) : ISagaTransaction
{
    public bool IsCommitted { get; private set; }
    public bool IsRolledback { get; private set; }
    public bool IsDisposed { get; private set; }

    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        if (onTransactionComplete != null)
        {
            await onTransactionComplete.Invoke();
        }

        IsCommitted = true;
    }

    public Task RollbackAsync(CancellationToken cancellationToken)
    {
        IsRolledback = true;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return ValueTask.CompletedTask;
    }
}
