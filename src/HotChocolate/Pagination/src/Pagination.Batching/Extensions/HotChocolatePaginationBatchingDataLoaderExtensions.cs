using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text;
using GreenDonut.Projections;
using HotChocolate.Pagination;

namespace GreenDonut;

/// <summary>
/// Provides extension methods to pass a pagination context to a DataLoader.
/// </summary>
public static class HotChocolatePaginationBatchingDataLoaderExtensions
{
    /// <summary>
    /// Branches a DataLoader with the provided <see cref="PagingArguments"/>.
    /// </summary>
    /// <param name="dataLoader">
    /// The DataLoader that shall be branched.
    /// </param>
    /// <param name="pagingArguments">
    /// The paging arguments that shall be exist as state in the branched DataLoader.
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
    public static IPagingDataLoader<TKey, TValue> WithPagingArguments<TKey, TValue>(
        this IDataLoader<TKey, TValue> dataLoader,
        PagingArguments pagingArguments)
        where TKey : notnull
    {
        if (dataLoader is null)
        {
            throw new ArgumentNullException(nameof(dataLoader));
        }

        var branchKey = CreateBranchKey(pagingArguments);
        return (IPagingDataLoader<TKey, TValue>)dataLoader.Branch(branchKey, CreatePagingDataLoader, pagingArguments);

        static IDataLoader CreatePagingDataLoader(
            string branchKey,
            IDataLoader<TKey, TValue> root,
            PagingArguments pagingArguments)
        {
            var branch = new PagingDataLoader<TKey, TValue>(
                (DataLoaderBase<TKey, TValue>)root,
                branchKey);
            branch.SetState(pagingArguments);
            return branch;
        }
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
#if NET8_0_OR_GREATER
    [Experimental(Experiments.Projections)]
#endif
    public static IPagingDataLoader<TKey, TValue> Select<TKey, TValue>(
        this IPagingDataLoader<TKey, TValue> dataLoader,
        Expression<Func<TValue, TValue>> selector)
        where TKey : notnull
    {
        if (dataLoader is null)
        {
            throw new ArgumentNullException(nameof(dataLoader));
        }

        if (selector is null)
        {
            throw new ArgumentNullException(nameof(selector));
        }

        var builder = dataLoader.GetOrSetState(_ => new DefaultSelectorBuilder<TValue>());
        builder.Add(selector);
        return dataLoader;
    }

    private static string CreateBranchKey(
        PagingArguments pagingArguments)
    {
        var key = new StringBuilder();
        key.Append(pagingArguments.First);
        key.Append(pagingArguments.After);
        key.Append(pagingArguments.Last);
        key.Append(pagingArguments.Before);
        return key.ToString();
    }
}
