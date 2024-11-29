using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public class SqlLiteCursorTestBase
{
    private DatabaseContext<TResult> BuildContext<TResult>(
        DatabaseContext<TResult> dbContext,
        params TResult[] results)
        where TResult : class
    {
        dbContext.Database.EnsureDeleted();
        dbContext.Database.EnsureCreated();

        var set = dbContext.Set<TResult>();

        foreach (var result in results)
        {
            set.Add(result);
            dbContext.SaveChanges();
        }

        return dbContext;
    }

    protected T[] CreateEntity<T>(params T[] entities) => entities;

    protected IRequestExecutor CreateSchema<TEntity>(TEntity[] entities)
        where TEntity : class
    {
        var builder = SchemaBuilder.New()
            .AddQueryType(
                c =>
                {
                    c.Name("Query");

                    c.Field("root")
                        .Resolve(
                            ctx =>
                            {
                                var context = ctx.Service<DatabaseContext<TEntity>>();
                                BuildContext(context, entities);
                                return context.Data;
                            })
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
                                    catch (Exception ex)
                                    {
                                        context.ContextData["sql"] = ex.Message;
                                    }
                                }
                            })
                        .UsePaging<ObjectType<TEntity>>(
                            options: new()
                            {
                                IncludeTotalCount = true,
                            });

                    c.Field("root1")
                        .Resolve(
                            ctx =>
                            {
                                var context = ctx.Service<DatabaseContext<TEntity>>();
                                BuildContext(context, entities);
                                return context.Data.ToArray().AsQueryable();
                            })
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
                                    catch (Exception ex)
                                    {
                                        context.ContextData["sql"] = ex.Message;
                                    }
                                }
                            });
                });

        var schema = builder.Create();

        return new ServiceCollection()
            .Configure<RequestExecutorSetup>(
                Schema.DefaultName,
                o => o.Schema = schema)
            .AddDbContextPool<DatabaseContext<TEntity>>(
                b => b.UseSqlite($"Data Source={Guid.NewGuid():N}.db"))
            .AddGraphQL()
            .UseDefaultPipeline()
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IRequestExecutorResolver>()
            .GetRequestExecutorAsync()
            .Result;
    }
}
