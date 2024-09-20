using HotChocolate.Data.Sorting.Expressions;
using HotChocolate.Resolvers;

namespace HotChocolate.Data;

/// <summary>
/// Extensions for sorting for <see cref="IEnumerable{T}"/> and <see cref="IQueryable{T}"/>
/// </summary>
public static class QueryableSortExtensions
{
    /// <summary>
    /// Sorts the selection set of the request onto the queryable.
    /// </summary>
    /// <param name="queryable">The queryable</param>
    /// <param name="context">
    /// The resolver context of the resolver that is annotated with UseSorting
    /// </param>
    /// <returns>The sorted queryable</returns>
    public static IQueryable<T> Sort<T>(
        this IQueryable<T> queryable,
        IResolverContext context) =>
        ExecuteSort(queryable, context, typeof(IQueryable<T>));

    /// <summary>
    /// Sorts the selection set of the request onto the enumerable.
    /// </summary>
    /// <param name="enumerable">The enumerable</param>
    /// <param name="context">
    /// The resolver context of the resolver that is annotated with UseSorting
    /// </param>
    /// <returns>The sorted enumerable</returns>
    public static IEnumerable<T> Sort<T>(
        this IEnumerable<T> enumerable,
        IResolverContext context) =>
        ExecuteSort(enumerable, context, typeof(IEnumerable<T>));

    /// <summary>
    /// Sorts the selection set of the request onto the enumerable.
    /// </summary>
    /// <param name="enumerable">The enumerable</param>
    /// <param name="context">
    /// The resolver context of the resolver that is annotated with UseSorting
    /// </param>
    /// <returns>The sorted enumerable</returns>
    public static IQueryableExecutable<T> Sort<T>(
        this IQueryableExecutable<T> enumerable,
        IResolverContext context) =>
        ExecuteSort(enumerable, context, typeof(IQueryableExecutable<T>));

    private static T ExecuteSort<T>(
        this T input,
        IResolverContext context,
        Type expectedType)
    {
        if (context.LocalContextData.TryGetValue(
                QueryableSortProvider.ContextApplySortingKey,
                out var applicatorObj) &&
            applicatorObj is ApplySorting applicator)
        {
            var resultObj = applicator(context, input);
            if (resultObj is T result)
            {
                return result;
            }

            throw ThrowHelper.Sorting_TypeMismatch(
                context,
                expectedType,
                resultObj!.GetType());
        }

        throw ThrowHelper.Sorting_SortingWasNotFound(context);
    }
}
