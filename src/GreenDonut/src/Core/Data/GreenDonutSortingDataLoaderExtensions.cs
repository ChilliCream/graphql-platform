namespace GreenDonut.Data;

/// <summary>
/// Provides DataLoader extension methods for <see cref="SortDefinition{TValue}"/>.
/// </summary>
public static class GreenDonutSortingDataLoaderExtensions
{
    /// <summary>
    /// Branches a DataLoader and adds a sort definition to the DataLoader state.
    /// </summary>
    /// <param name="dataLoader">
    /// The data loader.
    /// </param>
    /// <param name="sortDefinition">
    /// The sort definition that shall be added to the DataLoader state.
    /// </param>
    /// <typeparam name="TKey">
    /// The key type.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The value type.
    /// </typeparam>
    /// <returns>
    /// Returns a new data loader that applies the sort definition.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="dataLoader"/> is <c>null</c>.
    /// </exception>
    public static IDataLoader<TKey, TValue> OrderBy<TKey, TValue>(
        this IDataLoader<TKey, TValue> dataLoader,
        SortDefinition<TValue>? sortDefinition)
        where TKey : notnull
    {
        if (dataLoader is null)
        {
            throw new ArgumentNullException(nameof(dataLoader));
        }

        if (sortDefinition is null)
        {
            return dataLoader;
        }

        var branchKey = sortDefinition.ComputeHash();
        var state = new QueryState(DataLoaderStateKeys.Sorting, sortDefinition);
        return (IQueryDataLoader<TKey, TValue>)dataLoader.Branch(branchKey, DataLoaderStateHelper.CreateBranch, state);
    }

    /// <summary>
    /// Branches a DataLoader and adds a sort definition to the DataLoader state.
    /// </summary>
    /// <param name="dataLoader">
    /// The data loader.
    /// </param>
    /// <param name="sortDefinition">
    /// The sort definition that shall be added to the DataLoader state.
    /// </param>
    /// <typeparam name="TKey">
    /// The key type.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The value type.
    /// </typeparam>
    /// <returns>
    /// Returns a new data loader that applies the sort definition.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="dataLoader"/> is <c>null</c>.
    /// </exception>
    public static IDataLoader<TKey, TValue[]> OrderBy<TKey, TValue>(
        this IDataLoader<TKey, TValue[]> dataLoader,
        SortDefinition<TValue>? sortDefinition)
        where TKey : notnull
    {
        if (dataLoader is null)
        {
            throw new ArgumentNullException(nameof(dataLoader));
        }

        if (sortDefinition is null)
        {
            return dataLoader;
        }

        var branchKey = sortDefinition.ComputeHash();
        var state = new QueryState(DataLoaderStateKeys.Sorting, sortDefinition);
        return (IQueryDataLoader<TKey, TValue[]>)dataLoader.Branch(branchKey, DataLoaderStateHelper.CreateBranch, state);
    }

    /// <summary>
    /// Branches a DataLoader and adds a sort definition to the DataLoader state.
    /// </summary>
    /// <param name="dataLoader">
    /// The data loader.
    /// </param>
    /// <param name="sortDefinition">
    /// The sort definition that shall be added to the DataLoader state.
    /// </param>
    /// <typeparam name="TKey">
    /// The key type.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The value type.
    /// </typeparam>
    /// <returns>
    /// Returns a new data loader that applies the sort definition.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="dataLoader"/> is <c>null</c>.
    /// </exception>
    public static IDataLoader<TKey, List<TValue>> OrderBy<TKey, TValue>(
        this IDataLoader<TKey, List<TValue>> dataLoader,
        SortDefinition<TValue>? sortDefinition)
        where TKey : notnull
    {
        if (dataLoader is null)
        {
            throw new ArgumentNullException(nameof(dataLoader));
        }

        if (sortDefinition is null)
        {
            return dataLoader;
        }

        var branchKey = sortDefinition.ComputeHash();
        var state = new QueryState(DataLoaderStateKeys.Sorting, sortDefinition);
        return (IQueryDataLoader<TKey, List<TValue>>)dataLoader.Branch(branchKey, DataLoaderStateHelper.CreateBranch, state);
    }
}
