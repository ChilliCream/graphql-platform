using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Squadron;

namespace HotChocolate.Data.Projections.Spatial
{
    public class ProjectionVisitorTestBase
    {
        private readonly PostgreSqlResource<PostgisConfig> _resource;

        public ProjectionVisitorTestBase(PostgreSqlResource<PostgisConfig> resource)
        {
            _resource = resource;
        }

        private async Task<Func<IResolverContext, IEnumerable<T>>> BuildResolverAsync<T>(
            params T[] results)
            where T : class
        {
            var databaseName = Guid.NewGuid().ToString("N");
            var dbContext = new DatabaseContext<T>(_resource, databaseName);

            var sql = dbContext.Database.GenerateCreateScript();
            await _resource.CreateDatabaseAsync(databaseName);
            await _resource.RunSqlScriptAsync(
                "CREATE EXTENSION postgis;\n" + sql,
                databaseName);
            dbContext.AddRange(results);
            dbContext.SaveChanges();

            return ctx => dbContext.Data.AsQueryable();
        }

        protected async Task<IRequestExecutor> CreateSchemaAsync<TEntity>(
            TEntity[] entities,
            ProjectionConvention? convention = null)
            where TEntity : class
        {
            Func<IResolverContext, IEnumerable<TEntity>> resolver =
                await BuildResolverAsync(entities);

            return await new ServiceCollection()
                .AddGraphQL()
                .AddProjections()
                .AddSpatialTypes()
                .AddSpatialProjections()
                .AddQueryType(
                    c => c
                        .Name("Query")
                        .Field("root")
                        .Resolver(resolver)
                        .UseProjection()
                        .Use(next => async context =>
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
                        }))
                .UseRequest(next => async context =>
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
                .BuildRequestExecutorAsync();
        }
    }
}
