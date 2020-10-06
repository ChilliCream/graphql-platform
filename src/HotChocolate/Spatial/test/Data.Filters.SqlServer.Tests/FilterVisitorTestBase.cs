using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Data;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Data.Spatial.Filters;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Squadron;

namespace HotChocolate.Spatial.Data.Filters
{
    public class FilterVisitorTestBase
    {
        private PostgreSqlResource<PostgisConfig> _resouce;

        public FilterVisitorTestBase(PostgreSqlResource<PostgisConfig> resouce)
        {
            _resouce = resouce;
        }

        private Func<IResolverContext, IEnumerable<TResult>> BuildResolver<TResult>(
            params TResult[] results)
            where TResult : class
        {
            var dbContext = new DatabaseContext<TResult>(_resouce);

            try
            {
                var sql = dbContext.Database.GenerateCreateScript();
                _resouce.RunSqlScriptAsync("CREATE EXTENSION postgis;\n" + sql, "postgis")
                    .GetAwaiter()
                    .GetResult();
                dbContext.AddRange(results);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            dbContext.SaveChanges();

            return ctx => dbContext.Data.AsQueryable();
        }

        protected T[] CreateEntity<T>(params T[] entities) => entities;

        protected IRequestExecutor CreateSchema<TEntity, T>(
            TEntity[] entities,
            FilterConvention? convention = null)
            where TEntity : class
            where T : FilterInputType<TEntity>
        {
            Func<IResolverContext, IEnumerable<TEntity>>? resolver = BuildResolver(entities);

            ISchemaBuilder builder = SchemaBuilder.New()
                .AddSpatialTypes()
                .AddFiltering(
                    x => x
                        .AddDefaults()
                        .AddSpatialOperations()
                        .BindSpatialTypes()
                        .Provider(
                            new QueryableFilterProvider(
                                p => p.AddSpatialHandlers().AddDefaultFieldHandlers())))
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
                                            "EF Core 3.1 does not support ToQueryString officially";
                                    }
                                }
                            })
                        .UseFiltering<T>());

            ISchema? schema = builder.Create();

            return new ServiceCollection()
                .Configure<RequestExecutorFactoryOptions>(
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
