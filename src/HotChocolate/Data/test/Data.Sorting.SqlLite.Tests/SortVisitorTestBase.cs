using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Sorting;

public class SortVisitorTestBase
{
    private Func<IResolverContext, IEnumerable<TResult>> BuildResolver<TResult>(
        params TResult?[] results)
        where TResult : class
    {
        var dbContext = new DatabaseContext<TResult>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.EnsureCreated();

        var set = dbContext.Set<TResult>();

        foreach (var result in results)
        {
            if (result is not null)
            {
                set.Add(result);
                dbContext.SaveChanges();
            }
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

        var resolver = BuildResolver(entities);

        var builder = SchemaBuilder.New()
            .AddConvention<ISortConvention>(convention)
            .AddSorting()
            .AddQueryType(
                c =>
                {
                    ApplyConfigurationToField<TEntity, T>(
                        c.Name("Query").Field("root").Resolve(resolver),
                        false);

                    ApplyConfigurationToField<TEntity, T>(
                        c.Name("Query")
                            .Field("rootExecutable")
                            .Resolve(
                                ctx => resolver(ctx).AsExecutable()),
                        false);
                });

        var schema = builder.Create();

        return new ServiceCollection()
            .Configure<RequestExecutorSetup>(
                Schema.DefaultName,
                o => o.Schema = schema)
            .AddGraphQL()
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
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IRequestExecutorResolver>()
            .GetRequestExecutorAsync()
            .Result;
    }

    private void ApplyConfigurationToField<TEntity, TType>(
        IObjectFieldDescriptor field,
        bool withPaging)
        where TEntity : class
        where TType : SortInputType<TEntity>
    {
        field.Use(
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
                            "EF Core 3.1 does not support ToQueryString";
                    }
                }
            });

        if (withPaging)
        {
            field.UsePaging<ObjectType<TEntity>>();
        }

        field.UseSorting<TType>();
    }
}
