using GreenDonut.Data.Internal;

// ReSharper disable once CheckNamespace
namespace GreenDonut.Data;

/// <summary>
/// Provides extension method to the <see cref="DataLoaderFetchContext{TValue}"/> for pagination.
/// </summary>
public static class GreenDonutDataLoaderFetchContext
{
    /// <summary>
    /// Gets the <see cref="PagingArguments"/> from the DataLoader fetch context.
    /// </summary>
    /// <param name="context">
    /// The DataLoader fetch context.
    /// </param>
    /// <typeparam name="TValue">
    /// The value type of the DataLoader.
    /// </typeparam>
    /// <returns>
    /// Returns the <see cref="PagingArguments"/> from the DataLoader fetch context.
    /// </returns>
    public static PagingArguments GetPagingArguments<TValue>(
        this DataLoaderFetchContext<TValue> context)
        => context.GetRequiredState<PagingArguments>(DataLoaderStateKeys.PagingArgs);

    /// <summary>
    /// Gets the selector builder from the DataLoader state snapshot.
    /// The state builder can be user to create a selector expression.
    /// </summary>
    /// <typeparam name="TValue">
    /// The value type of the DataLoader.
    /// </typeparam>
    /// <returns>
    /// Returns the selector builder if it exists.
    /// </returns>
    public static ISelectorBuilder GetSelector<TValue>(
        this DataLoaderFetchContext<TValue> context)
    {
        if (context.ContextData.TryGetValue(DataLoaderStateKeys.Selector, out var value)
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
    /// <typeparam name="TValue">
    /// The value type of the DataLoader.
    /// </typeparam>
    /// <returns>
    /// Returns the predicate builder if it exists.
    /// </returns>
    public static IPredicateBuilder GetPredicate<TValue>(
        this DataLoaderFetchContext<TValue> context)
    {
        if (context.ContextData.TryGetValue(DataLoaderStateKeys.Predicate, out var value)
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
    /// <typeparam name="TValue">
    /// The value type.
    /// </typeparam>
    /// <returns>
    /// Returns the sorting definition if it exists.
    /// </returns>
    public static SortDefinition<TValue> GetSorting<TValue>(
        this DataLoaderFetchContext<TValue> context)
        => context.GetSorting<TValue, TValue>();

    /// <summary>
    /// Gets the sorting definition from the DataLoader state snapshot.
    /// </summary>
    /// <typeparam name="TEntity">
    /// The entity type.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The value type of the DataLoader.
    /// </typeparam>
    /// <returns>
    /// Returns the sorting definition if it exists.
    /// </returns>
    public static SortDefinition<TEntity> GetSorting<TValue, TEntity>(
        this DataLoaderFetchContext<TValue> context)
    {
        if (context.ContextData.TryGetValue(DataLoaderStateKeys.Sorting, out var value)
            && value is SortDefinition<TEntity> casted)
        {
            return casted;
        }

        return SortDefinition<TEntity>.Empty;
    }

    /// <summary>
    /// Gets the query context from the DataLoader state snapshot.
    /// </summary>
    /// <typeparam name="TValue">
    /// The value type of the DataLoader.
    /// </typeparam>
    /// <returns>
    /// Returns the query context if it exists, otherwise am empty query context.
    /// </returns>
    public static QueryContext<TValue> GetQueryContext<TValue>(
        this DataLoaderFetchContext<TValue> context)
        => context.GetQueryContext<TValue, TValue>();

    /// <summary>
    /// Gets the query context from the DataLoader state snapshot.
    /// </summary>
    /// <typeparam name="TEntity">
    /// The entity type.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The value type of the DataLoader.
    /// </typeparam>
    /// <returns>
    /// Returns the query context if it exists, otherwise am empty query context.
    /// </returns>
    public static QueryContext<TEntity> GetQueryContext<TValue, TEntity>(
        this DataLoaderFetchContext<TValue> context)
    {
        ISelectorBuilder? selector = null;
        IPredicateBuilder? predicate = null;
        SortDefinition<TEntity>? sorting = null;

        if (context.ContextData.TryGetValue(DataLoaderStateKeys.Selector, out var value)
            && value is ISelectorBuilder casted1)
        {
            selector = casted1;
        }

        if (context.ContextData.TryGetValue(DataLoaderStateKeys.Predicate, out value)
            && value is IPredicateBuilder casted2)
        {
            predicate = casted2;
        }

        if (context.ContextData.TryGetValue(DataLoaderStateKeys.Sorting, out value)
            && value is SortDefinition<TEntity> casted3)
        {
            sorting = casted3;
        }

        var selectorExpression = selector?.TryCompile<TEntity>();
        var predicateExpression = predicate?.TryCompile<TEntity>();

        if (selectorExpression is null
            && predicateExpression is null
            && sorting is null)
        {
            return QueryContext<TEntity>.Empty;
        }

        return new QueryContext<TEntity>(
            selectorExpression,
            predicateExpression,
            sorting);
    }
}
