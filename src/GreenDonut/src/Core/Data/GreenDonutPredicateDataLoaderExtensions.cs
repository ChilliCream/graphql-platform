using System.Collections.Immutable;
using System.Linq.Expressions;

namespace GreenDonut.Data;

/// <summary>
/// Data loader extensions for predicates.
/// </summary>
public static class GreenDonutPredicateDataLoaderExtensions
{
    /// <summary>
    /// Branches a DataLoader and applies a predicate to filter the data.
    /// </summary>
    /// <param name="dataLoader">
    /// The DataLoader to branch.
    /// </param>
    /// <param name="predicate">
    /// The data predicate.
    /// </param>
    /// <typeparam name="TKey">
    /// The key type.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The value type.
    /// </typeparam>
    /// <returns>
    /// Returns a branched DataLoader with the predicate applied.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="dataLoader"/> is <c>null</c>.
    /// </exception>
    public static IDataLoader<TKey, TValue> Where<TKey, TValue>(
        this IDataLoader<TKey, TValue> dataLoader,
        Expression<Func<TValue, bool>>? predicate)
        where TKey : notnull
    {
        if (dataLoader is null)
        {
            throw new ArgumentNullException(nameof(dataLoader));
        }

        if (predicate is null)
        {
            return dataLoader;
        }

        var branchKey = predicate.ComputeHash();
        var state = new QueryState(DataStateKeys.Predicate, GetOrCreateBuilder(dataLoader.ContextData, predicate));
        return (IQueryDataLoader<TKey, TValue>)dataLoader.Branch(branchKey, DataStateHelper.CreateBranch, state);
    }

    public static IDataLoader<TKey, TValue[]> Where<TKey, TValue>(
        this IDataLoader<TKey, TValue[]> dataLoader,
        Expression<Func<TValue, bool>>? predicate)
        where TKey : notnull
    {
        if (dataLoader is null)
        {
            throw new ArgumentNullException(nameof(dataLoader));
        }

        if (predicate is null)
        {
            return dataLoader;
        }

        var branchKey = predicate.ComputeHash();
        var state = new QueryState(DataStateKeys.Predicate, GetOrCreateBuilder(dataLoader.ContextData, predicate));
        return (IQueryDataLoader<TKey, TValue[]>)dataLoader.Branch(branchKey, DataStateHelper.CreateBranch, state);
    }

    public static IDataLoader<TKey, List<TValue>> Where<TKey, TValue>(
        this IDataLoader<TKey, List<TValue>> dataLoader,
        Expression<Func<TValue, bool>>? predicate)
        where TKey : notnull
    {
        if (dataLoader is null)
        {
            throw new ArgumentNullException(nameof(dataLoader));
        }

        if (predicate is null)
        {
            return dataLoader;
        }
        var branchKey = predicate.ComputeHash();
        var state = new QueryState(DataStateKeys.Predicate, GetOrCreateBuilder(dataLoader.ContextData, predicate));
        return (IQueryDataLoader<TKey, List<TValue>>)dataLoader.Branch(branchKey, DataStateHelper.CreateBranch, state);
    }

    internal static DefaultPredicateBuilder GetOrCreateBuilder<TValue>(
        IImmutableDictionary<string, object?> contextData,
        Expression<Func<TValue, bool>> predicate)
    {
        DefaultPredicateBuilder? builder;
        if (contextData.TryGetValue(DataStateKeys.Predicate, out var value))
        {
            builder = (DefaultPredicateBuilder)value!;
            builder = builder.Branch();
            builder.Add(predicate);
        }
        else
        {
            builder = new DefaultPredicateBuilder(predicate);
        }

        return builder;
    }
}
