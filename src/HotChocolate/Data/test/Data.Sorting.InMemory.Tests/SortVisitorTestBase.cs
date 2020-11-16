using System;
using System.Collections.Generic;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using System.Linq;

namespace HotChocolate.Data.Sorting
{
    public class SortVisitorTestBase
    {
        private Func<IResolverContext, IEnumerable<TResult>> BuildResolver<TResult>(
            params TResult[] results)
            where TResult : class
        {
            return ctx => results.AsQueryable();
        }

        protected T[] CreateEntity<T>(params T[] entities) => entities;

        protected IRequestExecutor CreateSchema<TEntity, T>(
            TEntity?[] entities,
            SortConvention? convention = null)
            where TEntity : class
            where T : SortInputType<TEntity>
        {
            convention ??= new SortConvention(x => x.AddDefaults().BindRuntimeType<TEntity, T>());

            Func<IResolverContext, IEnumerable<TEntity>>? resolver = BuildResolver(entities!);

            ISchemaBuilder builder = SchemaBuilder.New()
                .AddConvention<ISortConvention>(convention)
                .AddSorting()
                .AddQueryType(
                    c =>
                    {
                        c
                            .Name("Query")
                            .Field("root")
                            .Resolver(resolver)
                            .UseSorting<T>();

                        c
                            .Name("Query")
                            .Field("rootExecutable")
                            .Resolver(ctx => resolver(ctx).AsExecutable())
                            .UseSorting<T>();
                    });

            ISchema? schema = builder.Create();

            return schema.MakeExecutable();
        }
    }
}
