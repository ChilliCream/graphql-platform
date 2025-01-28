namespace GreenDonut.Data;

/// <summary>
/// Provides DataLoader extension methods for <see cref="QueryContext{TValue}"/>.
/// </summary>
public static class GreenDonutQueryContextDataLoaderExtensions
{
    /// <summary>
    /// Branches a DataLoader and adds a query context to the DataLoader state.
    /// </summary>
    /// <param name="dataLoader">
    /// The data loader.
    /// </param>
    /// <param name="context">
    /// The query context that shall be added to the DataLoader state.
    /// </param>
    /// <typeparam name="TKey">
    /// The key type.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The value type.
    /// </typeparam>
    /// <returns>
    /// Returns a new data loader that applies the query context.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="dataLoader"/> is <c>null</c>.
    /// </exception>
    public static IDataLoader<TKey, TValue> With<TKey, TValue>(
        this IDataLoader<TKey, TValue> dataLoader,
        QueryContext<TValue>? context)
        where TKey : notnull
    {
        if (dataLoader is null)
        {
            throw new ArgumentNullException(nameof(dataLoader));
        }

        if (context is null
            || (context.Selector is null
                && context.Predicate is null
                && context.Sorting is null))
        {
            return dataLoader;
        }

        var branchKey = context.ComputeHash();
        return (IQueryDataLoader<TKey, TValue>)dataLoader.Branch(branchKey, DataLoaderStateHelper.CreateBranch, context);
    }

    /// <summary>
    /// Branches a DataLoader and adds a query context to the DataLoader state.
    /// </summary>
    /// <param name="dataLoader">
    /// The data loader.
    /// </param>
    /// <param name="context">
    /// The query context that shall be added to the DataLoader state.
    /// </param>
    /// <typeparam name="TKey">
    /// The key type.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The value type.
    /// </typeparam>
    /// <returns>
    /// Returns a new data loader that applies the query context.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="dataLoader"/> is <c>null</c>.
    /// </exception>
    public static IDataLoader<TKey, TValue[]> With<TKey, TValue>(
        this IDataLoader<TKey, TValue[]> dataLoader,
        QueryContext<TValue>? context)
        where TKey : notnull
    {
        if (dataLoader is null)
        {
            throw new ArgumentNullException(nameof(dataLoader));
        }

        if (context is null
            || (context.Selector is null
                && context.Predicate is null
                && context.Sorting is null))
        {
            return dataLoader;
        }

        var branchKey = context.ComputeHash();
        return (IQueryDataLoader<TKey, TValue[]>)dataLoader.Branch(branchKey, DataLoaderStateHelper.CreateBranch, context);
    }

    /// <summary>
    /// Branches a DataLoader and adds a query context to the DataLoader state.
    /// </summary>
    /// <param name="dataLoader">
    /// The data loader.
    /// </param>
    /// <param name="context">
    /// The query context that shall be added to the DataLoader state.
    /// </param>
    /// <typeparam name="TKey">
    /// The key type.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The value type.
    /// </typeparam>
    /// <returns>
    /// Returns a new data loader that applies the query context.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="dataLoader"/> is <c>null</c>.
    /// </exception>
    public static IDataLoader<TKey, List<TValue>> With<TKey, TValue>(
        this IDataLoader<TKey, List<TValue>> dataLoader,
        QueryContext<TValue>? context)
        where TKey : notnull
    {
        if (dataLoader is null)
        {
            throw new ArgumentNullException(nameof(dataLoader));
        }

        if (context is null
            || (context.Selector is null
                && context.Predicate is null
                && context.Sorting is null))
        {
            return dataLoader;
        }

        var branchKey = context.ComputeHash();
        return (IQueryDataLoader<TKey, List<TValue>>)dataLoader.Branch(branchKey, DataLoaderStateHelper.CreateBranch, context);
    }
}
