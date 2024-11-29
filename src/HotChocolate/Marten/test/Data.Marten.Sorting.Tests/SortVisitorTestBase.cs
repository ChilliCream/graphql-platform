using HotChocolate.Data.Sorting;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Marten;
using Marten.Linq;
using Microsoft.Extensions.DependencyInjection;
using Squadron;

namespace HotChocolate.Data;

public sealed class ResourceContainer : IAsyncDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private int _testClassInstances = 0;

    public PostgreSqlResource Resource { get; } = new();

    public async ValueTask InitializeAsync()
    {
        await _semaphore.WaitAsync();

        try
        {
            if (_testClassInstances == 0)
            {
                await Resource.InitializeAsync();
            }
            _testClassInstances++;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _semaphore.WaitAsync();

        try
        {
            if (--_testClassInstances == 0)
            {
                await Resource.DisposeAsync();
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

public class SortVisitorTestBase : IAsyncLifetime
{
    protected static ResourceContainer Container { get; } = new();

    public async Task InitializeAsync() => await Container.InitializeAsync();

    public async Task DisposeAsync() => await Container.DisposeAsync();

    private static async Task<Func<IResolverContext, IQueryable<TResult>>> BuildResolverAsync<TResult>(
        IDocumentStore store,
        params TResult[] results)
        where TResult : class
    {
        await using var session = store.LightweightSession();

        foreach (var item in results)
        {
            session.Store(item);
        }

        await session.SaveChangesAsync();

        return ctx => ((IDocumentSession)ctx.LocalContextData["session"]!).Query<TResult>();
    }

    protected T[] CreateEntity<T>(params T[] entities) => entities;

    protected async Task<IRequestExecutor> CreateSchemaAsync<TEntity, T>(
        TEntity[] entities,
        SortConvention? convention = null)
        where TEntity : class
        where T : SortInputType<TEntity>
    {
        var dbName = $"DB_{Guid.NewGuid():N}";
        await Container.Resource.CreateDatabaseAsync(dbName);
        var store = DocumentStore.For(Container.Resource.GetConnectionString(dbName));

        var resolver = await BuildResolverAsync(store, entities);

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

        return await new ServiceCollection()
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
            .ModifyRequestOptions(x => x.IncludeExceptionDetails = true)
            .UseDefaultPipeline()
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IRequestExecutorResolver>()
            .GetRequestExecutorAsync();
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
            await using var session = store.LightweightSession();
            context.LocalContextData = context.LocalContextData.SetItem("session", session);
            await next(context);
        });

        field.Use(next => async context =>
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
