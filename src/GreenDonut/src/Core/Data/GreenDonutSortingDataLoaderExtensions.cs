namespace GreenDonut.Data;

public static class GreenDonutSortingDataLoaderExtensions
{
    public static IDataLoader<TKey, TValue> Order<TKey, TValue>(
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
        var state = new QueryState(DataStateKeys.Sorting, sortDefinition);
        return (IQueryDataLoader<TKey, TValue>)dataLoader.Branch(branchKey, DataStateHelper.CreateBranch, state);
    }

    public static IDataLoader<TKey, TValue[]> Order<TKey, TValue>(
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
        var state = new QueryState(DataStateKeys.Sorting, sortDefinition);
        return (IQueryDataLoader<TKey, TValue[]>)dataLoader.Branch(branchKey, DataStateHelper.CreateBranch, state);
    }

    public static IDataLoader<TKey, List<TValue>> Order<TKey, TValue>(
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
        var state = new QueryState(DataStateKeys.Sorting, sortDefinition);
        return (IQueryDataLoader<TKey, List<TValue>>)dataLoader.Branch(branchKey, DataStateHelper.CreateBranch, state);
    }
}
