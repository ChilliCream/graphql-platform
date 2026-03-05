namespace Mocha.Sagas.Tests;

public class TestSagaStore(Func<ValueTask>? onTransactionComplete = null) : ISagaStore
{
    public List<TestSagaTransaction> Transactions { get; } = new();
    public List<SagaStateBase> States { get; } = new();

    public Task<ISagaTransaction> StartTransactionAsync(CancellationToken cancellationToken)
    {
        var transaction = new TestSagaTransaction(onTransactionComplete);
        Transactions.Add(transaction);
        return Task.FromResult<ISagaTransaction>(transaction);
    }

    public Task SaveAsync<T>(Saga saga, T state, CancellationToken cancellationToken) where T : SagaStateBase
    {
        var existing = States.FirstOrDefault(x => x.Id == state.Id);

        if (existing is not null)
        {
            States.Remove(existing);
        }

        States.Add(state);

        return Task.CompletedTask;
    }

    public Task DeleteAsync(Saga saga, Guid id, CancellationToken cancellationToken)
    {
        var existing = States.FirstOrDefault(x => x.Id == id);

        if (existing is not null)
        {
            States.Remove(existing);
        }

        return Task.CompletedTask;
    }

    public Task<T?> LoadAsync<T>(Saga saga, Guid id, CancellationToken cancellationToken)
    {
        var state = States.FirstOrDefault(x => x.Id == id);
        if (state is T s)
        {
            return Task.FromResult<T?>(s);
        }

        return Task.FromResult<T?>(default);
    }
}
