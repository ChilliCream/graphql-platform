using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Marten;
using Marten.Linq;
using Microsoft.Extensions.DependencyInjection;
using Squadron;

namespace HotChocolate.Data.Sorting;

public class SortVisitorTestBase : IAsyncLifetime
{
    protected PostgreSqlResource Resource { get; } = new();

    public Task InitializeAsync() => Resource.InitializeAsync();

    public Task DisposeAsync() => Resource.DisposeAsync();

    private Func<IResolverContext, IQueryable<TResult>> BuildResolver<TResult>(
        IDocumentStore store,
        params TResult[] results)
        where TResult : class
    {
        using var session = store.LightweightSession();

        foreach (var item in results)
        {
            session.Store(item);
        }

        session.SaveChanges();

        return ctx => ((IDocumentSession)ctx.LocalContextData["session"]!).Query<TResult>();
    }

    protected T[] CreateEntity<T>(params T[] entities) => entities;

    protected IRequestExecutor CreateSchema<TEntity, T>(
        TEntity[] entities,
        SortConvention? convention = null)
        where TEntity : class
        where T : SortInputType<TEntity>
    {
        var dbName = $"DB_{Guid.NewGuid():N}";
        Resource.CreateDatabaseAsync(dbName).GetAwaiter().GetResult();
        var store = DocumentStore.For(Resource.GetConnectionString(dbName));

        var resolver = BuildResolver(store, entities);

        var builder = SchemaBuilder.New()
            .AddMartenSorting()
            .AddQueryType(
                c =>
                {
                    ApplyConfigurationToField<TEntity, T>(
                        store,
                        c.Name("Query").Field("root").Resolve(resolver),
                        false);

                    ApplyConfigurationToField<TEntity, T>(
                        store,
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
                            QueryResultBuilder
                                .FromResult(context.Result!.ExpectQueryResult())
                                .SetContextData("sql", queryString)
                                .Create();
                    }
                })
            .ModifyRequestOptions(x => x.IncludeExceptionDetails = true)
            .UseDefaultPipeline()
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IRequestExecutorResolver>()
            .GetRequestExecutorAsync()
            .Result;
    }

    private void ApplyConfigurationToField<TEntity, TType>(
        IDocumentStore store,
        IObjectFieldDescriptor field,
        bool withPaging)
        where TEntity : class
        where TType : SortInputType<TEntity>
    {
        field.Use(next => async context =>
        {
            await using (var session = store.LightweightSession())
            {
                context.LocalContextData = context.LocalContextData.SetItem("session", session);
                await next(context);
            }
        });
        field.Use(
            next => async context =>
            {
                await next(context);

                if (context.Result is IMartenQueryable<TEntity> queryable)
                {
                    context.ContextData["sql"] = queryable.ToCommand().CommandText;
                    context.Result = await queryable.ToListAsync(context.RequestAborted);
                }
            });

        if (withPaging)
        {
            field.UsePaging<ObjectType<TEntity>>();
        }

        field.UseSorting<TType>();
    }
}
