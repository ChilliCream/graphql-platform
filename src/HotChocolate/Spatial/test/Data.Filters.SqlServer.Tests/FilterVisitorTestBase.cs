using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Squadron;

namespace HotChocolate.Data.Spatial.Filters;

public class FilterVisitorTestBase
{
    private readonly PostgreSqlResource<PostgisConfig> _resource;

    public FilterVisitorTestBase(PostgreSqlResource<PostgisConfig> resource)
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

        var set = dbContext.Set<T>();

        foreach (var result in results)
        {
            set.Add(result);
            await dbContext.SaveChangesAsync();
        }

        return _ => dbContext.Data.AsQueryable();
    }

    protected async Task<IRequestExecutor> CreateSchemaAsync<TEntity, T>(
        TEntity[] entities,
        FilterConvention? convention = null)
        where TEntity : class
        where T : FilterInputType<TEntity>
    {
        var resolver = await BuildResolverAsync(entities);

        return await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddSpatialTypes()
            .AddSpatialFiltering()
            .AddQueryType(
                c => c
                    .Name("Query")
                    .Field("root")
                    .Resolve(resolver)
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
                    .UseFiltering<T>())
            .UseRequest(
                next => async context =>
                {
                    await next(context);
                    if (context.ContextData.TryGetValue("sql", out var queryString))
                    {
                        context.Result =
                            OperationResultBuilder
                                .FromResult(context.Result!.ExpectOperationResult())
                                .SetContextData("sql", queryString)
                                .Build();
                    }
                })
            .UseDefaultPipeline()
            .BuildRequestExecutorAsync();
    }
}
