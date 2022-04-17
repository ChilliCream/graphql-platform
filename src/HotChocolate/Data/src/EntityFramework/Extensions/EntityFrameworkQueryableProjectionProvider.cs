using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using HotChocolate.Data.Projections;
using HotChocolate.Data.Projections.Expressions;
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

    /// <inheritdoc />
    protected override ApplyProjection CreateApplicatorAsync<TEntityType>()
    {
        return (context, input) =>
        {
            if (input is null)
            {
                return input;
            }

            // if projections are already applied we can skip
            var skipProjection =
                context.LocalContextData.TryGetValue(SkipProjectionKey, out object? skip) &&
                skip is true;

            // ensure sorting is only applied once
            context.LocalContextData =
                context.LocalContextData.SetItem(SkipProjectionKey, true);

            if (skipProjection)
            {
                return input;
            }

            var visitorContext =
                new QueryableProjectionContext(
                    context,
                    context.ObjectType,
                    context.Selection.Type.UnwrapRuntimeType(),
                    input is EntityQueryable<TEntityType>);
            var visitor = new QueryableProjectionVisitor();
            visitor.Visit(visitorContext);

            Expression<Func<TEntityType, TEntityType>> projection =
                visitorContext.Project<TEntityType>();

            input = input switch
            {
                IQueryable<TEntityType> q => q.Select(projection),
                IEnumerable<TEntityType> e => e.AsQueryable().Select(projection),
                QueryableExecutable<TEntityType> ex =>
                    ex.WithSource(ex.Source.Select(projection)),
                _ => input
            };

            return input;
        };
    }
}
