using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution.Processing;

// ReSharper disable once CheckNamespace
namespace System.Linq;

/// <summary>
/// Provides extension methods to integrate <see cref="IQueryable{T}"/>
/// with <see cref="Selection"/> and <see cref="IFilterContext"/>.
/// </summary>
public static class HotChocolateDataQueryableExtensions
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
    /// Throws if <paramref name="selection"/> is <c>null</c>.
    /// </exception>
    public static IQueryable<T> Select<T>(this IQueryable<T> queryable, Selection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
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
    /// Throws if <paramref name="filter"/> is <c>null</c>.
    /// </exception>
    public static IQueryable<T> Where<T>(this IQueryable<T> queryable, IFilterContext filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
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
    /// Throws if <paramref name="sorting"/> is <c>null</c>.
    /// </exception>
    public static IQueryable<T> Order<T>(this IQueryable<T> queryable, ISortingContext sorting)
    {
        ArgumentNullException.ThrowIfNull(sorting);

        var sortDefinition = sorting.AsSortDefinition<T>();

        if (sortDefinition is null || sortDefinition.Operations.Length == 0)
        {
            return queryable;
        }

        return queryable.OrderBy(sortDefinition);
    }
}
