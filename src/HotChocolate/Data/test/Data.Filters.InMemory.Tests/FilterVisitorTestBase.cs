using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Filters;

public class FilterVisitorTestBase
{
    private readonly object _lock = new();

    private Func<IResolverContext, IEnumerable<TResult>> BuildResolver<TResult>(
        params TResult[] results)
        where TResult : class
    {
        return _ => results.AsQueryable();
    }

    protected T[] CreateEntity<T>(params T[] entities) => entities;

    protected IRequestExecutor CreateSchema<TEntity, T>(
        TEntity[] entities,
        FilterConvention? convention = null,
        bool withPaging = false,
        Action<IRequestExecutorBuilder>? configure = null)
        where TEntity : class
        where T : FilterInputType<TEntity>
    {
        convention ??= new FilterConvention(x
            => new QueryableFilterConventionDescriptor(x).AddCaseInsensitiveContains()
                .AddDefaults()
                .BindRuntimeType<TEntity, T>());

        var resolver = BuildResolver(entities);

        var builder = new ServiceCollection()
            .AddGraphQL()
            .AddConvention<IFilterConvention>(convention)
            .AddQueryableFiltering(x => x.AddCaseInsensitiveContains())
            .AddQueryType(
                c =>
                {
                    var field = c
                        .Name("Query")
                        .Field("root")
                        .Resolve(resolver);

                    if (withPaging)
                    {
                        field.UsePaging<ObjectType<TEntity>>();
                    }

                    field.UseFiltering<T>();

                    field = c
                        .Name("Query")
                        .Field("rootExecutable")
                        .Resolve(ctx => resolver(ctx).AsExecutable());

                    if (withPaging)
                    {
                        field.UsePaging<ObjectType<TEntity>>();
                    }

                    field.UseFiltering<T>();
                });

        configure?.Invoke(builder);

        return builder.BuildRequestExecutorAsync().GetAwaiter().GetResult();
    }
}
