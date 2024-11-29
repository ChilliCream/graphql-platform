using HotChocolate.Data.Filters;
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
}
