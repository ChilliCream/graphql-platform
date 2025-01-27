using HotChocolate.Data;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution.Processing;

// ReSharper disable once CheckNamespace
namespace System.Linq;

/// <summary>
/// Provides extension methods to integrate <see cref="IQueryable{T}"/>
/// with <see cref="ISelection"/> and <see cref="IFilterContext"/>.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Applies a selection to the queryable.
    /// </summary>
    /// <param name="queryable">
    /// The queryable that shall be projected.
    /// </param>
    /// <param name="selection">
    /// The selection that shall be applied to the queryable.
    /// </param>
    /// <typeparam name="T">
    /// The type of the queryable.
    /// </typeparam>
    /// <returns>
    /// Returns a queryable that has the selection applied.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="queryable"/> is <c>null</c> or if <paramref name="selection"/> is <c>null</c>.
    /// </exception>
    public static IQueryable<T> Select<T>(this IQueryable<T> queryable, ISelection selection)
    {
        if (queryable is null)
        {
            throw new ArgumentNullException(nameof(queryable));
        }

        if (selection is null)
        {
            throw new ArgumentNullException(nameof(selection));
        }

        return queryable.Select(selection.AsSelector<T>());
    }

    /// <summary>
    /// Applies a filter context to the queryable.
    /// </summary>
    /// <param name="queryable">
    /// The queryable that shall be filtered.
    /// </param>
    /// <param name="filter">
    /// The filter context that shall be applied to the queryable.
    /// </param>
    /// <typeparam name="T">
    /// The type of the queryable.
    /// </typeparam>
    /// <returns>
    /// Returns a queryable that has the filter applied.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="queryable"/> is <c>null</c> or if <paramref name="filter"/> is <c>null</c>.
    /// </exception>
    public static IQueryable<T> Where<T>(this IQueryable<T> queryable, IFilterContext filter)
    {
        if (queryable is null)
        {
            throw new ArgumentNullException(nameof(queryable));
        }

        if (filter is null)
        {
            throw new ArgumentNullException(nameof(filter));
        }

        var predicate = filter.AsPredicate<T>();
        return predicate is null ? queryable : queryable.Where(predicate);
    }

    /// <summary>
    /// Applies a sorting context to the queryable.
    /// </summary>
    /// <param name="queryable">
    /// The queryable that shall be sorted.
    /// </param>
    /// <param name="sorting">
    /// The sorting context that shall be applied to the queryable.
    /// </param>
    /// <typeparam name="T">
    /// The type of the queryable.
    /// </typeparam>
    /// <returns>
    /// Returns a queryable that has the sorting applied.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="queryable"/> is <c>null</c> or if <paramref name="sorting"/> is <c>null</c>.
    /// </exception>
    public static IQueryable<T> Order<T>(this IQueryable<T> queryable, ISortingContext sorting)
    {
        if (queryable is null)
        {
            throw new ArgumentNullException(nameof(queryable));
        }

        if (sorting is null)
        {
            throw new ArgumentNullException(nameof(sorting));
        }

        var sortDefinition = sorting.AsSortDefinition<T>();

        if (sortDefinition is null || sortDefinition.Operations.Length == 0)
        {
            return queryable;
        }

        return queryable.Order(sortDefinition);
    }

    private static IQueryable<T> Order<T>(this IQueryable<T> queryable, SortDefinition<T> sortDefinition)
    {
        if (queryable is null)
        {
            throw new ArgumentNullException(nameof(queryable));
        }

        if (sortDefinition is null)
        {
            throw new ArgumentNullException(nameof(sortDefinition));
        }

        if (sortDefinition.Operations.Length == 0)
        {
            return queryable;
        }

        var first = sortDefinition.Operations[0];
        var query = first.ApplyOrderBy(queryable);

        for (var i = 1; i < sortDefinition.Operations.Length; i++)
        {
            query = sortDefinition.Operations[i].ApplyThenBy(query);
        }

        return query;
    }

    /// <summary>
    /// Applies a data context to the queryable.
    /// </summary>
    /// <param name="queryable">
    /// The queryable that shall be projected, filtered and sorted.
    /// </param>
    /// <param name="dataContext">
    /// The data context that shall be applied to the queryable.
    /// </param>
    /// <typeparam name="T">
    /// The type of the queryable.
    /// </typeparam>
    /// <returns>
    /// Returns a queryable that has the data context applied.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="queryable"/> is <c>null</c> or if <paramref name="dataContext"/> is <c>null</c>.
    /// </exception>
    public static IQueryable<T> Apply<T>(this IQueryable<T> queryable, DataContext<T> dataContext)
    {
        if (queryable is null)
        {
            throw new ArgumentNullException(nameof(queryable));
        }

        if (dataContext is null)
        {
            throw new ArgumentNullException(nameof(dataContext));
        }

        if (dataContext.Selector is not null)
        {
            queryable = queryable.Select(dataContext.Selector);
        }

        if (dataContext.Predicate is not null)
        {
            queryable = queryable.Where(dataContext.Predicate);
        }

        if (dataContext.Sorting is not null)
        {
            queryable = queryable.Order(dataContext.Sorting);
        }

        return queryable;
    }
}
