using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using HotChocolate.Data.Projections.Expressions;
using HotChocolate.Resolvers;
using static HotChocolate.Data.Projections.Expressions.QueryableProjectionProvider;

namespace HotChocolate.Data;

/// <summary>
/// Common extensions for automapper and <see cref="IQueryable{T}"/>
/// </summary>
public static class AutoMapperQueryableExtensions
{
    /// <summary>
    /// Extension method to project from a queryable using the <see cref="IResolverContext"/>
    /// to project <typeparamref name="TSource"/> into <typeparamref name="TResult"/> based on
    /// the GraphQL selection.
    /// </summary>
    /// <param name="queryable">The Queryable that holds the selection</param>
    /// <param name="context">The resolver context of the resolver</param>
    /// <typeparam name="TSource">The source type</typeparam>
    /// <typeparam name="TResult">The result type</typeparam>
    /// <returns>The projected queryable</returns>
    public static IQueryable<TResult> ProjectTo<TSource, TResult>(
        this IQueryable<TSource> queryable,
        IResolverContext context)
    {
        IMapper mapper = context.Service<IMapper>();

        // ensure projections are only applied once
        context.LocalContextData = context.LocalContextData.SetItem(SkipProjectionKey, true);

        QueryableProjectionContext visitorContext =
            new(context, context.ObjectType, context.Selection.Field.Type.UnwrapRuntimeType());

        QueryableProjectionVisitor.Default.Visit(visitorContext);

#pragma warning disable CS8631
        var membersToExpand = visitorContext.GetMembersToExpand<TResult, object?>();
#pragma warning restore CS8631

        return queryable.ProjectTo(mapper.ConfigurationProvider, membersToExpand.ToArray());
    }

    private static IEnumerable<Expression<Func<T, TTarget>>> GetMembersToExpand<T, TTarget>(
        this QueryableProjectionContext context)
        where T : TTarget
    {
        if (context.TryGetQueryableScope(out var scope))
        {
            foreach (var assignment in scope.Level.Peek())
            {
                var expression = Expression.Convert(assignment.Expression, typeof(object));
                yield return Expression.Lambda<Func<T, TTarget>>(expression, scope.Parameter);
            }
        }
    }
}
