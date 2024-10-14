using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace GreenDonut.Predicates;

/// <summary>
/// Data loader extensions for predicates.
/// </summary>
[Experimental(Experiments.Predicates)]
public static class PredicateDataLoaderExtensions
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

        if (dataLoader.ContextData.TryGetValue(typeof(IPredicateBuilder).FullName!, out var value))
        {
            var context = (DefaultPredicateBuilder)value!;
            context.Add(predicate);
            return dataLoader;
        }

        var branchKey = predicate.ToString();
        return (IDataLoader<TKey, TValue>)dataLoader.Branch(branchKey, CreateBranch, predicate);

        static IDataLoader CreateBranch(
            string key,
            IDataLoader<TKey, TValue> dataLoader,
            Expression<Func<TValue, bool>> predicate)
        {
            var branch = new PredicateDataLoader<TKey, TValue>(
                (DataLoaderBase<TKey, TValue>)dataLoader,
                key);
            var context = new DefaultPredicateBuilder();
            branch.ContextData =
                branch.ContextData.SetItem(typeof(IPredicateBuilder).FullName!, context);
            context.Add(predicate);
            return branch;
        }
    }

    /// <summary>
    /// Applies the predicate from the DataLoader state to a queryable.
    /// </summary>
    /// <param name="query">
    /// The queryable to apply the predicate to.
    /// </param>
    /// <param name="builder">
    /// The predicate builder.
    /// </param>
    /// <typeparam name="T">
    /// The queryable type.
    /// </typeparam>
    /// <returns>
    /// Returns a query with the predicate applied, ready to fetch data with the key.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="query"/> is <c>null</c>.
    /// </exception>
    public static IQueryable<T> Where<T>(
        this IQueryable<T> query,
        IPredicateBuilder builder)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        var predicate = builder.TryCompile<T>();

        if (predicate is not null)
        {
            query = query.Where(predicate);
        }

        return query;
    }
}
