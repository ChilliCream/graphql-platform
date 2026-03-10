using System.Collections.Concurrent;

namespace Mocha.Sagas;

/// <summary>
/// Singleton storage for in-memory saga states.
/// </summary>
public sealed class InMemorySagaStateStorage
{
    private readonly ConcurrentDictionary<(string SagaName, Guid Id), SagaStateBase> _states = new();

    /// <summary>
    /// Saves a saga state to in-memory storage, creating or overwriting an existing entry.
    /// </summary>
    /// <param name="sagaName">The name of the saga.</param>
    /// <param name="id">The unique identifier of the saga instance.</param>
    /// <param name="state">The saga state to save.</param>
    public void Save(string sagaName, Guid id, SagaStateBase state)
    {
        _states[(sagaName, id)] = state;
    }

    /// <summary>
    /// Deletes a saga state from in-memory storage.
    /// </summary>
    /// <param name="sagaName">The name of the saga.</param>
    /// <param name="id">The unique identifier of the saga instance to delete.</param>
    public void Delete(string sagaName, Guid id)
    {
        _states.TryRemove((sagaName, id), out _);
    }

    /// <summary>
    /// Loads a saga state from in-memory storage.
    /// </summary>
    /// <typeparam name="T">The type to cast the state to.</typeparam>
    /// <param name="sagaName">The name of the saga.</param>
    /// <param name="id">The unique identifier of the saga instance to load.</param>
    /// <returns>The saga state cast to <typeparamref name="T"/>, or <c>default</c> if not found or not of the expected type.</returns>
    public T? Load<T>(string sagaName, Guid id)
    {
        if (_states.TryGetValue((sagaName, id), out var state) && state is T typed)
        {
            return typed;
        }

        return default;
    }

    /// <summary>
    /// Gets the number of saga states currently stored.
    /// </summary>
    public int Count => _states.Count;

    /// <summary>
    /// Clears all stored saga states.
    /// </summary>
    public void Clear() => _states.Clear();
}
