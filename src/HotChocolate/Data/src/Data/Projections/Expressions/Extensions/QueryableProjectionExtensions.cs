using HotChocolate.Data.Projections.Expressions;
using HotChocolate.Resolvers;

namespace HotChocolate.Data;

/// <summary>
/// Extensions for projection for <see cref="IEnumerable{T}"/> and <see cref="IQueryable{T}"/>
/// </summary>
public static class QueryableProjectExtensions
{
    /// <summary>
    /// Projects the selection set of the request onto the queryable.
    /// </summary>
    /// <param name="queryable">The queryable</param>
    /// <param name="context">
    /// The resolver context of the resolver that is annotated with UseProjection
    /// </param>
    /// <returns>The projected queryable</returns>
    public static IQueryable<T> Project<T>(
        this IQueryable<T> queryable,
        IResolverContext context) =>
        ExecuteProject(queryable, context, typeof(IQueryable<T>));

    /// <summary>
    /// Projects the selection set of the request onto the enumerable.
    /// </summary>
    /// <param name="enumerable">The enumerable</param>
    /// <param name="context">
    /// The resolver context of the resolver that is annotated with UseProjection
    /// </param>
    /// <returns>The projected enumerable</returns>
    public static IEnumerable<T> Project<T>(
        this IEnumerable<T> enumerable,
        IResolverContext context) =>
        ExecuteProject(enumerable, context, typeof(IEnumerable<T>));

    /// <summary>
    /// Projects the selection set of the request onto the enumerable.
    /// </summary>
    /// <param name="enumerable">The enumerable</param>
    /// <param name="context">
    /// The resolver context of the resolver that is annotated with UseProjection
    /// </param>
    /// <returns>The projected enumerable</returns>
    public static IQueryableExecutable<T> Project<T>(
        this IQueryableExecutable<T> enumerable,
        IResolverContext context) =>
        ExecuteProject(enumerable, context, typeof(IQueryableExecutable<T>));

    private static T ExecuteProject<T>(
        this T input,
        IResolverContext context,
        Type expectedType)
    {
        if (context.LocalContextData.TryGetValue(
            QueryableProjectionProvider.ContextApplyProjectionKey,
            out var applicatorObj) &&
            applicatorObj is ApplyProjection applicator)
        {
            var resultObj = applicator(context, input);
            if (resultObj is T result)
            {
                return result;
            }

            throw ThrowHelper.Projection_TypeMismatch(
                context,
                expectedType,
                resultObj!.GetType());
        }

        throw ThrowHelper.Projection_ProjectionWasNotFound(context);
    }
}
