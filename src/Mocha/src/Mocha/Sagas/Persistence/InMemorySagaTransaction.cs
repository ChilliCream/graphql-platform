namespace Mocha.Sagas;

/// <summary>
/// An in-memory transaction that stages changes and applies them on commit.
/// </summary>
internal sealed class InMemorySagaTransaction : ISagaTransaction
{
    private readonly InMemorySagaStateStorage _storage;
    private readonly Dictionary<(string SagaName, Guid Id), SagaStateBase?> _stagedChanges = new();
    private bool _isActive = true;

    public InMemorySagaTransaction(InMemorySagaStateStorage storage)
    {
        _storage = storage;
    }

    public bool IsActive => _isActive;

    public void StageSave(string sagaName, Guid id, SagaStateBase state)
    {
        if (!_isActive)
        {
            throw new InvalidOperationException("Transaction is no longer active.");
        }

        _stagedChanges[(sagaName, id)] = state;
    }

    public void StageDelete(string sagaName, Guid id)
    {
        if (!_isActive)
        {
            throw new InvalidOperationException("Transaction is no longer active.");
        }

        _stagedChanges[(sagaName, id)] = null;
    }

    public bool TryGetStagedState<T>(string sagaName, Guid id, out T? state)
    {
        if (_stagedChanges.TryGetValue((sagaName, id), out var staged))
        {
            if (staged is T typed)
            {
                state = typed;
                return true;
            }

            // Staged for deletion
            state = default;
            return true;
        }

        state = default;
        return false;
    }

    public Task CommitAsync(CancellationToken cancellationToken)
    {
        if (!_isActive)
        {
            return Task.CompletedTask;
        }

        _isActive = false;

        foreach (var ((sagaName, id), state) in _stagedChanges)
        {
            if (state is null)
            {
                _storage.Delete(sagaName, id);
            }
            else
            {
                _storage.Save(sagaName, id, state);
            }
        }

        _stagedChanges.Clear();

        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken cancellationToken)
    {
        if (!_isActive)
        {
            return Task.CompletedTask;
        }

        _isActive = false;
        _stagedChanges.Clear();

        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        if (_isActive)
        {
            _isActive = false;
            _stagedChanges.Clear();
        }

        return ValueTask.CompletedTask;
    }
}
