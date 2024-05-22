using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Marten;
using Marten.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public abstract class FilterVisitorTestBase : IAsyncLifetime
{
    protected static ResourceContainer Container { get; } = new();

    public async Task InitializeAsync() => await Container.InitializeAsync();

    public async Task DisposeAsync() => await Container.DisposeAsync();

    protected T[] CreateEntity<T>(params T[] entities) => entities;

    protected IRequestExecutor CreateSchema<TEntity, T>(
        TEntity[] entities,
        FilterConvention? convention = null,
        bool withPaging = false,
        Action<ISchemaBuilder>? configure = null)
        where TEntity : class
        where T : FilterInputType<TEntity>
    {
        var dbName = $"DB_{Guid.NewGuid():N}";
        Container.Resource.CreateDatabaseAsync(dbName).GetAwaiter().GetResult();
        var store = DocumentStore.For(Container.Resource.GetConnectionString(dbName));

        var resolver = BuildResolver(store, entities);

        var builder = SchemaBuilder.New()
            .AddMartenFiltering()
            .AddQueryType(
                c =>
                {
                    ApplyConfigurationToField<TEntity, T>(
                        store,
                        c.Name("Query").Field("root").Resolve(resolver),
                        withPaging);

                    ApplyConfigurationToField<TEntity, T>(
                        store,
                        c.Name("Query")
                            .Field("rootExecutable")
                            .Resolve(
                                ctx => resolver(ctx).AsExecutable()),
                        withPaging);
                });

        configure?.Invoke(builder);

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
                                .FromResult(context.Result!.ExpectQueryResult())
                                .SetContextData("sql", queryString)
                                .Build();
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
        where TType : FilterInputType<TEntity>
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

        field.UseFiltering<TType>();
    }

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
}
