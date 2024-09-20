using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Resolvers;

namespace HotChocolate.Data;

/// <summary>
/// Extensions for filtering for <see cref="IEnumerable{T}"/> and <see cref="IQueryable{T}"/>
/// </summary>
public static class QueryableFilterExtensions
{
    /// <summary>
    /// Filters the selection set of the request onto the queryable.
    /// </summary>
    /// <param name="queryable">The queryable</param>
    /// <param name="context">
    /// The resolver context of the resolver that is annotated with UseFiltering
    /// </param>
    /// <returns>The filtered queryable</returns>
    public static IQueryable<T> Filter<T>(
        this IQueryable<T> queryable,
        IResolverContext context) =>
        ExecuteFilter(queryable, context, typeof(IQueryable<T>));

    /// <summary>
    /// Filters the selection set of the request onto the enumerable.
    /// </summary>
    /// <param name="enumerable">The enumerable</param>
    /// <param name="context">
    /// The resolver context of the resolver that is annotated with UseFiltering
    /// </param>
    /// <returns>The filtered enumerable</returns>
    public static IEnumerable<T> Filter<T>(
        this IEnumerable<T> enumerable,
        IResolverContext context) =>
        ExecuteFilter(enumerable, context, typeof(IEnumerable<T>));

    /// <summary>
    /// Filters the selection set of the request onto the enumerable.
    /// </summary>
    /// <param name="enumerable">The enumerable</param>
    /// <param name="context">
    /// The resolver context of the resolver that is annotated with UseFiltering
    /// </param>
    /// <returns>The filtered enumerable</returns>
    public static IQueryableExecutable<T> Filter<T>(
        this IQueryableExecutable<T> enumerable,
        IResolverContext context) =>
        ExecuteFilter(enumerable, context, typeof(IQueryableExecutable<T>));

    private static T ExecuteFilter<T>(
        this T input,
        IResolverContext context,
        Type expectedType)
    {
        if (context.LocalContextData.TryGetValue(
            QueryableFilterProvider.ContextApplyFilteringKey,
            out var applicatorObj) &&
            applicatorObj is ApplyFiltering applicator)
        {
            var resultObj = applicator(context, input);
            if (resultObj is T result)
            {
                return result;
            }

            throw ThrowHelper.Filtering_TypeMismatch(
                context,
                expectedType,
                resultObj!.GetType());
        }

        throw ThrowHelper.Filtering_FilteringWasNotFound(context);
    }
}
