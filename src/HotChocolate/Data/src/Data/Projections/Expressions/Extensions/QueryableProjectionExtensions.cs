using System;
using System.Collections.Generic;
using System.Linq;
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
    public static LogicallyTypedCollectionT<T> Project<T>(
        this IQueryable<T> queryable,
        IResolverContext context) =>
        ExecuteProject<IQueryable<T>, T>(queryable, context);

    /// <summary>
    /// Projects the selection set of the request onto the enumerable.
    /// </summary>
    /// <param name="enumerable">The enumerable</param>
    /// <param name="context">
    /// The resolver context of the resolver that is annotated with UseProjection
    /// </param>
    /// <returns>The projected enumerable</returns>
    public static LogicallyTypedCollectionT<T> Project<T>(
        this IEnumerable<T> enumerable,
        IResolverContext context) =>
        ExecuteProject<IEnumerable<T>, T>(enumerable, context);

    /// <summary>
    /// Projects the selection set of the request onto the enumerable.
    /// </summary>
    /// <param name="enumerable">The enumerable</param>
    /// <param name="context">
    /// The resolver context of the resolver that is annotated with UseProjection
    /// </param>
    /// <returns>The projected enumerable</returns>
    public static LogicallyTypedCollectionT<T> Project<T>(
        this QueryableExecutable<T> enumerable,
        IResolverContext context) =>
        ExecuteProject<QueryableExecutable<T>, T>(enumerable, context);

    private static LogicallyTypedCollectionT<TLogicalElementType> ExecuteProject<T, TLogicalElementType>(
        this T input,
        IResolverContext context)

        where T : notnull
    {
        if (context.LocalContextData.TryGetValue(
            QueryableProjectionProvider.ContextApplyProjectionKey,
            out var applicatorObj) &&
            applicatorObj is ApplyProjection applicator)
        {
            var resultObj = applicator(context,
                new LogicallyTypedValue(input, typeof(TLogicalElementType), isCollection: true));

            if (resultObj.Value.LogicalElementType == typeof(TLogicalElementType)
                && typeof(T).GetGenericTypeDefinition().IsInstanceOfType(resultObj.Value)
                && resultObj.Value.IsCollection)
            {
                return new(resultObj.Value);
            }

            throw ThrowHelper.Projection_TypeMismatch(
                context,
                typeof(TLogicalElementType),
                resultObj.Value.LogicalElementType);
        }

        throw ThrowHelper.Projection_ProjectionWasNotFound(context);
    }
}
