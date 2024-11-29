using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using HotChocolate.Pagination;

// ReSharper disable once CheckNamespace
namespace GreenDonut.Predicates;

/// <summary>
/// Provides extension methods to pass a pagination context to a DataLoader.
/// </summary>
public static class HotChocolatePaginationBatchingDataLoaderPredicateExtensions
{
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
    [Experimental(Experiments.Predicates)]
    public static IPagingDataLoader<TKey, Page<TValue>> Where<TKey, TValue>(
        this IPagingDataLoader<TKey, Page<TValue>> dataLoader,
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

        if (dataLoader.ContextData.TryGetValue(typeof(IPredicateBuilder).FullName!, out var value))
        {
            var context = (DefaultPredicateBuilder)value!;
            context.Add(predicate);
            return dataLoader;
        }

        var branchKey = predicate.ToString();
        return (IPagingDataLoader<TKey, Page<TValue>>)dataLoader.Branch(
            branchKey,
            CreateBranch,
            predicate);

        static IDataLoader CreateBranch(
            string key,
            IDataLoader<TKey, Page<TValue>> dataLoader,
            Expression<Func<TValue, bool>> predicate)
        {
            var branch = new PagingDataLoader<TKey, Page<TValue>>(dataLoader, key);
            var context = new DefaultPredicateBuilder();
            branch.ContextData = branch.ContextData.SetItem(typeof(IPredicateBuilder).FullName!, context);
            context.Add(predicate);
            return branch;
        }
    }
}
