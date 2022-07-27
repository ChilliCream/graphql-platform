using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Sorting;

public class SortVisitorTestBase
{
    private Func<IResolverContext, IEnumerable<TResult>> BuildResolver<TResult>(
        params TResult?[] results)
        where TResult : class
    {
        return ctx => results.AsQueryable()!;
    }

    protected T[] CreateEntity<T>(params T[] entities) => entities;

    protected IRequestExecutor CreateSchema<TEntity, T>(
        TEntity?[] entities,
        SortConvention? convention = null,
        Action<IRequestExecutorBuilder>? configure = null)
        where TEntity : class
        where T : SortInputType<TEntity>
    {
        convention ??= new SortConvention(x => x.AddDefaults().BindRuntimeType<TEntity, T>());

        var resolver = BuildResolver(entities!);

        var builder = new ServiceCollection()
            .AddGraphQL()
            .AddConvention<ISortConvention>(convention)
            .AddQueryableSorting()
            .AddSorting()
            .AddQueryType(
                c =>
                {
                    c
                        .Name("Query")
                        .Field("root")
                        .Resolve(resolver)
                        .UseSorting<T>();

                    c
                        .Name("Query")
                        .Field("rootExecutable")
                        .Resolve(ctx => resolver(ctx).AsExecutable())
                        .UseSorting<T>();
                });

        configure?.Invoke(builder);

        return builder.BuildRequestExecutorAsync().GetAwaiter().GetResult();
    }
}
