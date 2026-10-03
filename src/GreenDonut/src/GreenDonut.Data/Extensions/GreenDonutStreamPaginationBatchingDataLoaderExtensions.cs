using System.Linq.Expressions;
using GreenDonut.Data.Internal;
using static GreenDonut.Data.GreenDonutPredicateDataLoaderExtensions;
using static GreenDonut.Data.Internal.DataLoaderStateHelper;

// ReSharper disable once CheckNamespace
namespace GreenDonut.Data;

/// <summary>
/// Provides extension methods to pass a pagination context to a DataLoader that streams its pages.
/// </summary>
public static class GreenDonutStreamPaginationBatchingDataLoaderExtensions
{
    /// <summary>
    /// Branches a DataLoader with the provided <see cref="PagingArguments"/>.
    /// </summary>
    /// <param name="dataLoader">
    /// The DataLoader that shall be branched.
    /// </param>
    /// <param name="pagingArguments">
    /// The paging arguments that shall exist as state in the branched DataLoader.
    /// </param>
    /// <param name="context">
    /// The query context that shall exist as state in the branched DataLoader.
    /// </param>
    /// <typeparam name="TKey">
    /// The key type of the DataLoader.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The value type of the DataLoader.
    /// </typeparam>
    /// <returns>
    /// Returns a branched DataLoader with the provided <see cref="PagingArguments"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Throws if the <paramref name="dataLoader"/> is <c>null</c>.
    /// </exception>
    public static IDataLoader<TKey, StreamPage<TValue>> With<TKey, TValue>(
        this IDataLoader<TKey, StreamPage<TValue>> dataLoader,
        PagingArguments pagingArguments,
        QueryContext<TValue>? context = null)
        where TKey : notnull
        => WithInternal(dataLoader, pagingArguments, context);

    private static IDataLoader<TKey, StreamPage<TValue>> WithInternal<TKey, TValue>(
        this IDataLoader<TKey, StreamPage<TValue>> dataLoader,
        PagingArguments pagingArguments,
        QueryContext<TValue>? context)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(dataLoader);

        var branchKey = pagingArguments.ComputeHash(context);
        var state = new PagingState<TValue>(pagingArguments, context);
        return (IQueryDataLoader<TKey, StreamPage<TValue>>)dataLoader.Branch(branchKey, CreateBranch, state);
    }

    /// <summary>
    /// Adds a projection as state to the DataLoader.
    /// </summary>
    /// <param name="dataLoader">
    /// The DataLoader.
    /// </param>
    /// <param name="selector">
    /// The projection that shall be added as state to the DataLoader.
    /// </param>
    /// <typeparam name="TKey">
    /// The key type of the DataLoader.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The value type of the DataLoader.
    /// </typeparam>
    /// <returns>
    /// Returns the DataLoader with the added projection.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Throws if the <paramref name="dataLoader"/> is <c>null</c>.
    /// </exception>
    public static IDataLoader<TKey, StreamPage<TValue>> Select<TKey, TValue>(
        this IDataLoader<TKey, StreamPage<TValue>> dataLoader,
        Expression<Func<TValue, TValue>>? selector)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(dataLoader);

        if (selector is null)
        {
            return dataLoader;
        }

        if (dataLoader.ContextData.TryGetValue(DataLoaderStateKeys.Selector, out var value))
        {
            var context = (DefaultSelectorBuilder)value!;
            context.Add(selector);
            return dataLoader;
        }

        var branchKey = selector.ComputeHash();
        var state = new QueryState(DataLoaderStateKeys.Selector, new DefaultSelectorBuilder(selector));
        return (IQueryDataLoader<TKey, StreamPage<TValue>>)dataLoader.Branch(branchKey, CreateBranch,
            state);
    }

    /// <summary>
    /// Adds a predicate as state to the DataLoader.
    /// </summary>
    /// <param name="dataLoader">
    /// The DataLoader.
    /// </param>
    /// <param name="predicate">
    /// The predicate that shall be added as state to the DataLoader.
    /// </param>
    /// <typeparam name="TKey">
    /// The key type of the DataLoader.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The value type of the DataLoader.
    /// </typeparam>
    /// <returns>
    /// Returns the DataLoader with the added projection.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Throws if the <paramref name="dataLoader"/> is <c>null</c>.
    /// </exception>
    public static IDataLoader<TKey, StreamPage<TValue>> Where<TKey, TValue>(
        this IDataLoader<TKey, StreamPage<TValue>> dataLoader,
        Expression<Func<TValue, bool>>? predicate)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(dataLoader);

        if (predicate is null)
        {
            return dataLoader;
        }

        var branchKey = predicate.ComputeHash();
        var state = new QueryState(DataLoaderStateKeys.Predicate,
            GetOrCreateBuilder(dataLoader.ContextData, predicate));
        return (IQueryDataLoader<TKey, StreamPage<TValue>>)dataLoader.Branch(branchKey, CreateBranch,
            state);
    }

    /// <summary>
    /// Adds a sorting definition as state to the DataLoader.
    /// </summary>
    /// <param name="dataLoader">
    /// The DataLoader.
    /// </param>
    /// <param name="sortDefinition">
    /// The sorting definition that shall be added as state to the DataLoader.
    /// </param>
    /// <typeparam name="TKey">
    /// The key type of the DataLoader.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The value type of the DataLoader.
    /// </typeparam>
    /// <returns>
    /// Returns the DataLoader with the added projection.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Throws if the <paramref name="dataLoader"/> is <c>null</c>.
    /// </exception>
    public static IDataLoader<TKey, StreamPage<TValue>> OrderBy<TKey, TValue>(
        this IDataLoader<TKey, StreamPage<TValue>> dataLoader,
        SortDefinition<TValue>? sortDefinition)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(dataLoader);

        if (sortDefinition is null)
        {
            return dataLoader;
        }

        var branchKey = sortDefinition.ComputeHash();
        var state = new QueryState(DataLoaderStateKeys.Sorting, sortDefinition);
        return (IQueryDataLoader<TKey, StreamPage<TValue>>)dataLoader.Branch(branchKey, CreateBranch,
            state);
    }
}
