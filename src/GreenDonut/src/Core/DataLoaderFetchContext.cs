using System.Collections.Immutable;
using GreenDonut.Data;

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
#if NET6_0_OR_GREATER

    /// <summary>
    /// Gets the selector builder from the DataLoader state snapshot.
    /// The state builder can be user to create a selector expression.
    /// </summary>
    /// <returns>
    /// Returns the selector builder if it exists.
    /// </returns>
    public ISelectorBuilder GetSelector()
    {
        if (ContextData.TryGetValue(DataLoaderStateKeys.Selector, out var value)
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
    public IPredicateBuilder GetPredicate()
    {
        if (ContextData.TryGetValue(DataLoaderStateKeys.Predicate, out var value)
            && value is IPredicateBuilder casted)
        {
            return casted;
        }

        // if no predicate was found we will just return
        // a new default predicate builder.
        return DefaultPredicateBuilder.Empty;
    }

    /// <summary>
    /// Gets the sorting definition from the DataLoader state snapshot.
    /// </summary>
    /// <typeparam name="T">
    /// The entity type.
    /// </typeparam>
    /// <returns>
    /// Returns the sorting definition if it exists.
    /// </returns>
    public SortDefinition<T> GetSorting<T>()
    {
        if (ContextData.TryGetValue(DataLoaderStateKeys.Sorting, out var value)
            && value is SortDefinition<T> casted)
        {
            return casted;
        }

        return SortDefinition<T>.Empty;
    }

    /// <summary>
    /// Gets the query context from the DataLoader state snapshot.
    /// </summary>
    /// <typeparam name="T">
    /// The entity type.
    /// </typeparam>
    /// <returns>
    /// Returns the query context if it exists, otherwise am empty query context.
    /// </returns>
    public QueryContext<T> GetQueryContext<T>()
    {
        ISelectorBuilder? selector = null;
        IPredicateBuilder? predicate = null;
        SortDefinition<T>? sorting = null;

        if (ContextData.TryGetValue(DataLoaderStateKeys.Selector, out var value)
            && value is ISelectorBuilder casted1)
        {
            selector = casted1;
        }

        if (ContextData.TryGetValue(DataLoaderStateKeys.Predicate, out value)
            && value is IPredicateBuilder casted2)
        {
            predicate = casted2;
        }

        if (ContextData.TryGetValue(DataLoaderStateKeys.Sorting, out value)
            && value is SortDefinition<T> casted3)
        {
            sorting = casted3;
        }

        var selectorExpression = selector?.TryCompile<T>();
        var predicateExpression = predicate?.TryCompile<T>();

        if (selectorExpression is null
            && predicateExpression is null
            && sorting is null)
        {
            return QueryContext<T>.Empty;
        }

        return new QueryContext<T>(
            selectorExpression,
            predicateExpression,
            sorting);
    }
#endif
}
