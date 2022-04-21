using System;
using System.Linq.Expressions;
using HotChocolate.Data.Projections;
using HotChocolate.Data.Projections.Expressions;
using HotChocolate.Resolvers;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace HotChocolate.Data.Extensions;

/// <summary>
/// A <see cref="EntityFrameworkQueryableProjectionProvider"/> translates a incoming query to
/// a IQueryable optimized for Entity Framework
/// </summary>
public class EntityFrameworkQueryableProjectionProvider : QueryableProjectionProvider
{
    /// <summary>
    /// Creates a new instance of <see cref="EntityFrameworkQueryableProjectionProvider"/>
    /// </summary>
    public EntityFrameworkQueryableProjectionProvider()
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="EntityFrameworkQueryableProjectionProvider"/>
    /// </summary>
    public EntityFrameworkQueryableProjectionProvider(
        Action<IProjectionProviderDescriptor> configure)
        : base(configure)
    {
    }

    protected override Expression<Func<TEntityType, TEntityType>> ConstructProjection<TEntityType>(IResolverContext context, object? input)
    {
        var visitorContext = new EntityFrameworkQueryableProjectionContext(
            context,
            context.ObjectType,
            context.Selection.Type.UnwrapRuntimeType(),
            input is EntityQueryable<TEntityType>);

        var visitor = new EntityFrameworkQueryableProjectionVisitor();
            visitor.Visit(visitorContext);

        return visitorContext.Project<TEntityType>();
    }
}
