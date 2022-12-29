using AutoMapper;
using AutoMapper.QueryableExtensions;
using HotChocolate.Data.Projections.Expressions;
using HotChocolate.Resolvers;
using System.Linq.Expressions;
using System.Reflection;

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
        context.LocalContextData = context.LocalContextData.SetItem(QueryableProjectionProvider.SkipProjectionKey, true);

        QueryableProjectionContext visitorContext =
            new(context, context.ObjectType, context.Selection.Field.Type.UnwrapRuntimeType());

        QueryableProjectionVisitor.Default.Visit(visitorContext);

#pragma warning disable CS8631
        Expression<Func<TResult, object?>> projection = visitorContext.Project<TResult, object?>();
#pragma warning restore CS8631

        var memberInfos = MemberVisitor.GetMemberPath(projection);
        var membersToExpand = new List<string>();

        for (var i = 0; i < memberInfos.Length; i++)
        {
            var current = memberInfos[i];
            var path = current.Name;
            for (var j = i; j >= 0; j--)
            {
                if (memberInfos[j].HasProperty(current))
                {
                    current = memberInfos[j];

                    path = $"{current.Name}.{path}";
                    if (memberInfos[j].ReflectedType == typeof(TResult))
                        break;
                }
            }
            membersToExpand.Add(path);
        }

        return queryable.ProjectTo<TResult>(mapper.ConfigurationProvider, null, membersToExpand.Distinct().ToArray());
    }

    private static bool HasProperty(this MemberInfo member, MemberInfo property)
    {
        return member.ReflectedType!.GetRuntimeProperties().Any(p =>
        p.PropertyType == property.ReflectedType ||
            (p.PropertyType.IsGenericType && p.PropertyType.GenericTypeArguments[0] == property.ReflectedType));
    }
}
