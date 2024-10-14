using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using GreenDonut.Predicates;
using GreenDonut.Selectors;

namespace GreenDonut;

/// <summary>
/// The fetch context is used to pass a snapshot of the transient DataLoader state into a fetch call.
/// This allows the fetch to interact with a save version of the state.
/// </summary>
/// <param name="contextData">
/// The context data that is passed into the fetch call.
/// </param>
/// <typeparam name="TValue">
/// The value type of the DataLoader.
/// </typeparam>
public readonly struct DataLoaderFetchContext<TValue>(
    IImmutableDictionary<string, object?> contextData)
{
    public IImmutableDictionary<string, object?> ContextData { get; } = contextData;

    /// <summary>
    /// Gets a value from the DataLoader state snapshot.
    /// </summary>
    /// <typeparam name="TState">
    /// The type of the state value.
    /// </typeparam>
    /// <returns>
    /// Returns the state value if it exists.
    /// </returns>
    public TState? GetState<TState>()
        => GetState<TState>(typeof(TState).FullName ?? typeof(TState).Name);

    /// <summary>
    /// Gets a value from the DataLoader state snapshot.
    /// </summary>
    /// <param name="key">
    /// The key to look up the value.
    /// </param>
    /// <typeparam name="TState">
    /// The type of the state value.
    /// </typeparam>
    /// <returns>
    /// Returns the state value if it exists.
    /// </returns>
    public TState? GetState<TState>(string key)
    {
        if (ContextData.TryGetValue(key, out var value) && value is TState state)
        {
            return state;
        }

        return default;
    }

    /// <summary>
    /// Gets a required value from the DataLoader state snapshot.
    /// </summary>
    /// <typeparam name="TState">
    /// The type of the state value.
    /// </typeparam>
    /// <returns>
    /// Returns the state value if it exists.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Throws an exception if the state value does not exist.
    /// </exception>
    public TState GetRequiredState<TState>()
        => GetRequiredState<TState>(typeof(TState).FullName ?? typeof(TState).Name);

    /// <summary>
    /// Gets a required value from the DataLoader state snapshot.
    /// </summary>
    /// <param name="key">
    /// The key to look up the value.
    /// </param>
    /// <typeparam name="TState">
    /// The type of the state value.
    /// </typeparam>
    /// <returns>
    /// Returns the state value if it exists.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Throws an exception if the state value does not exist.
    /// </exception>
    public TState GetRequiredState<TState>(string key)
    {
        if (ContextData.TryGetValue(key, out var value) && value is TState state)
        {
            return state;
        }

        throw new InvalidOperationException(
            $"The state `{key}` is not available on the DataLoader.");
    }

    /// <summary>
    /// Gets a value from the DataLoader state snapshot or returns a default value.
    /// </summary>
    /// <param name="defaultValue">
    /// The default value to return if the state value does not exist.
    /// </param>
    /// <typeparam name="TState">
    /// The type of the state value.
    /// </typeparam>
    /// <returns>
    /// Returns the state value if it exists.
    /// </returns>
    public TState GetStateOrDefault<TState>(TState defaultValue)
        => GetStateOrDefault(typeof(TState).FullName ?? typeof(TState).Name, defaultValue);

    /// <summary>
    /// Gets a value from the DataLoader state snapshot or returns a default value.
    /// </summary>
    /// <param name="key">
    /// The key to look up the value.
    /// </param>
    /// <param name="defaultValue">
    /// The default value to return if the state value does not exist.
    /// </param>
    /// <typeparam name="TState">
    /// The type of the state value.
    /// </typeparam>
    /// <returns>
    /// Returns the state value if it exists.
    /// </returns>
    public TState GetStateOrDefault<TState>(string key, TState defaultValue)
    {
        if (ContextData.TryGetValue(key, out var value) && value is TState state)
        {
            return state;
        }

        return defaultValue;
    }

    /// <summary>
    /// Gets the selector builder from the DataLoader state snapshot.
    /// The state builder can be user to create a selector expression.
    /// </summary>
    /// <returns>
    /// Returns the selector builder if it exists.
    /// </returns>
    [Experimental(Experiments.Selectors)]
    public ISelectorBuilder GetSelector()
    {
        if (ContextData.TryGetValue(typeof(ISelectorBuilder).FullName!, out var value)
            && value is ISelectorBuilder casted)
        {
            return casted;
        }

        // if no selector was found we will just return
        // a new default selector builder.
        return new DefaultSelectorBuilder();
    }

    /// <summary>
    /// Gets the predicate builder from the DataLoader state snapshot.
    /// The state builder can be used to create a predicate expression.
    /// </summary>
    /// <returns>
    /// Returns the predicate builder if it exists.
    /// </returns>
    [Experimental(Experiments.Predicates)]
    public IPredicateBuilder GetPredicate()
    {
        if (ContextData.TryGetValue(typeof(IPredicateBuilder).FullName!, out var value)
            && value is DefaultPredicateBuilder casted)
        {
            return casted;
        }

        // if no predicate was found we will just return
        // a new default predicate builder.
        return new DefaultPredicateBuilder();
    }
}
