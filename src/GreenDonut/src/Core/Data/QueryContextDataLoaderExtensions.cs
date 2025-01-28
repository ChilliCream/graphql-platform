namespace GreenDonut.Data;

public static class QueryContextDataLoaderExtensions
{
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
        return (IQueryDataLoader<TKey, TValue>)dataLoader.Branch(branchKey, DataStateHelper.CreateBranch, context);
    }

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
        return (IQueryDataLoader<TKey, TValue[]>)dataLoader.Branch(branchKey, DataStateHelper.CreateBranch, context);
    }

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
        return (IQueryDataLoader<TKey, List<TValue>>)dataLoader.Branch(branchKey, DataStateHelper.CreateBranch, context);
    }
}
