using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Relay;

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
        Action<ISchemaBuilder>? configure = null)
        where TEntity : class
        where T : FilterInputType<TEntity>
    {
        convention ??= new FilterConvention(x => x.AddDefaults().BindRuntimeType<TEntity, T>());

        Func<IResolverContext, IEnumerable<TEntity>> resolver = BuildResolver(entities);

        ISchemaBuilder builder = SchemaBuilder.New()
            .AddConvention<IFilterConvention>(convention)
            .AddFiltering()
            .AddQueryType(
                c =>
                {
                    IObjectFieldDescriptor field = c
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

        ISchema schema = builder.Create();

        return schema.MakeExecutable();
    }
}
