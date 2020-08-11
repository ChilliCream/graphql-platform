using System;
using System.Collections.Generic;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using System.Linq;

namespace HotChocolate.Data.Filters
{
    public class FilterVisitorTestBase
    {
        private readonly object _lock = new object();

        public FilterVisitorTestBase()
        {
        }

        private Func<IResolverContext, IEnumerable<TResult>> BuildResolver<TResult>(
            params TResult[] results)
            where TResult : class
        {
            return ctx => results.AsQueryable();
        }

        protected T[] CreateEntity<T>(params T[] entities) => entities;

        protected IRequestExecutor CreateSchema<TEntity, T>(
            TEntity[] entities,
            FilterConvention? convention = null)
            where TEntity : class
            where T : IFilterInputType
        {
            convention ??= new FilterConvention(x => x.UseDefault());

            Func<IResolverContext, IEnumerable<TEntity>>? resolver = BuildResolver(entities);

            ISchemaBuilder builder = SchemaBuilder.New()
                .AddConvention<IFilterConvention>(convention)
                .UseFiltering()
                .AddQueryType(c =>
                    c.Name("Query")
                        .Field("root")
                        .Resolver(resolver)
                        .UseFiltering<T>());

            ISchema? schema = builder.Create();

            return schema.MakeExecutable();
        }

    }
}

