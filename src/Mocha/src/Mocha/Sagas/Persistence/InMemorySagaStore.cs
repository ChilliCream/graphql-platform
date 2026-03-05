namespace Mocha.Sagas;

/// <summary>
/// A scoped in-memory implementation of <see cref="ISagaStore"/> for development, testing,
/// and scenarios where saga state persistence is not required across process restarts.
/// </summary>
public sealed class InMemorySagaStore : ISagaStore
{
    private readonly InMemorySagaStateStorage _storage;
    private InMemorySagaTransaction? _transaction;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemorySagaStore"/> class.
    /// </summary>
    /// <param name="storage">The shared in-memory state storage.</param>
    public InMemorySagaStore(InMemorySagaStateStorage storage)
    {
        _storage = storage;
    }

    /// <inheritdoc />
    public Task<ISagaTransaction> StartTransactionAsync(CancellationToken cancellationToken)
    {
        if (_transaction is { IsActive: true })
        {
            return Task.FromResult<ISagaTransaction>(NoOpSagaTransaction.Instance);
        }

        _transaction = new InMemorySagaTransaction(_storage);

        return Task.FromResult<ISagaTransaction>(_transaction);
    }

    /// <inheritdoc />
    public Task SaveAsync<T>(Saga saga, T state, CancellationToken cancellationToken) where T : SagaStateBase
    {
        if (_transaction is { IsActive: true })
        {
            _transaction.StageSave(saga.Name, state.Id, state);
        }
        else
        {
            _storage.Save(saga.Name, state.Id, state);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(Saga saga, Guid id, CancellationToken cancellationToken)
    {
        if (_transaction is { IsActive: true })
        {
            _transaction.StageDelete(saga.Name, id);
        }
        else
        {
            _storage.Delete(saga.Name, id);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<T?> LoadAsync<T>(Saga saga, Guid id, CancellationToken cancellationToken)
    {
        if (_transaction is { IsActive: true }
            && _transaction.TryGetStagedState<T>(saga.Name, id, out var staged))
        {
            return Task.FromResult(staged);
        }

        return Task.FromResult(_storage.Load<T>(saga.Name, id));
    }
}
