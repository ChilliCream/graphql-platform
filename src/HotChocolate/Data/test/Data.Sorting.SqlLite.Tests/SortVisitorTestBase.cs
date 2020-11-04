using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Sorting
{
    public class SortVisitorTestBase
    {
        private Func<IResolverContext, IEnumerable<TResult>> BuildResolver<TResult>(
            params TResult[] results)
            where TResult : class
        {
            var dbContext = new DatabaseContext<TResult>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
            dbContext.AddRange(results);

            try
            {
                dbContext.SaveChanges();
            }
            catch (Exception ex)
            {
            }

            return ctx => dbContext.Data.AsQueryable();
        }

        protected T[] CreateEntity<T>(params T[] entities) => entities;

        protected IRequestExecutor CreateSchema<TEntity, T>(
            TEntity?[] entities,
            SortConvention? convention = null)
            where TEntity : class
            where T : SortInputType<TEntity>
        {
            convention ??= new SortConvention(x => x.AddDefaults().BindRuntimeType<TEntity, T>());

            Func<IResolverContext, IEnumerable<TEntity>> resolver = BuildResolver(entities!);

            ISchemaBuilder builder = SchemaBuilder.New()
                .AddConvention<ISortConvention>(convention)
                .AddSorting()
                .AddQueryType(
                    c => c
                        .Name("Query")
                        .Field("root")
                        .Resolver(resolver)
                        .Use(
                            next => async context =>
                            {
                                await next(context);

                                if (context.Result is IQueryable<TEntity> queryable)
                                {
                                    try
                                    {
                                        context.ContextData["sql"] = queryable.ToQueryString();
                                    }
                                    catch (Exception)
                                    {
                                        context.ContextData["sql"] =
                                            "EF Core 3.1 does not support ToQuerString offically";
                                    }
                                }
                            })
                        .UseSorting<T>());

            ISchema? schema = builder.Create();

            return new ServiceCollection()
                .Configure<RequestExecutorSetup>(
                    Schema.DefaultName,
                    o => o.Schema = schema)
                .AddGraphQL()
                .UseRequest(
                    next => async context =>
                    {
                        await next(context);
                        if (context.Result is IReadOnlyQueryResult result &&
                            context.ContextData.TryGetValue("sql", out var queryString))
                        {
                            context.Result =
                                QueryResultBuilder
                                    .FromResult(result)
                                    .SetContextData("sql", queryString)
                                    .Create();
                        }
                    })
                .UseDefaultPipeline()
                .Services
                .BuildServiceProvider()
                .GetRequiredService<IRequestExecutorResolver>()
                .GetRequestExecutorAsync()
                .Result;
        }
    }
}
