using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;
using Squadron;

namespace HotChocolate.Data.Raven;

public abstract class VisitorTestBase : IAsyncLifetime
{
    public RavenDBResource<CustomRavenDBOptions> Resource { get; } = new();

    public Task InitializeAsync() => Resource.InitializeAsync();

    public Task DisposeAsync() => Resource.DisposeAsync();

    protected T[] CreateEntity<T>(params T[] entities) => entities;

    public IDocumentStore CreateDatabase()
    {
        var dbName = $"DB_{Guid.NewGuid():N}";

        return Resource.CreateDatabase(dbName);
    }

    protected IRequestExecutor CreateSchema<TEntity, T>(
        TEntity[] entities,
        FilterConvention? convention = null,
        bool withPaging = false,
        Action<IRequestExecutorBuilder>? configure = null)
        where TEntity : class
        where T : FilterInputType<TEntity>
    {
        var documentStore = CreateDatabase();

        var resolver = BuildResolver(documentStore, entities);

        var builder = new ServiceCollection()
            .AddSingleton<IDocumentStore>(documentStore)
            .AddGraphQLServer()
            .AddRavenFiltering()
            .AddRavenPagingProviders()
            .AddQueryType(
                c =>
                {
                    ApplyConfigurationToField<TEntity, T>(
                        documentStore,
                        c.Name("Query").Field("root").Resolve(resolver),
                        withPaging);

                    ApplyConfigurationToField<TEntity, T>(
                        documentStore,
                        c.Name("Query")
                            .Field("rootExecutable")
                            .Resolve(ctx => resolver(ctx).AsExecutable()),
                        withPaging);
                })
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
            .UseDefaultPipeline();

        configure?.Invoke(builder);

        return builder.BuildRequestExecutorAsync().GetAwaiter().GetResult();
    }

    private void ApplyConfigurationToField<TEntity, TType>(
        IDocumentStore store,
        IObjectFieldDescriptor field,
        bool withPaging)
        where TEntity : class
        where TType : FilterInputType<TEntity>
    {
        field.Use(
            next => async context =>
            {
                using (var session = store.OpenAsyncSession())
                {
                    context.LocalContextData = context.LocalContextData.SetItem("session", session);
                    await next(context);
                }
            });
        field.Use(
            next => async context =>
            {
                await next(context);

                if (context.Result is IRavenQueryable<TEntity> queryable)
                {
                    context.ContextData["sql"] = queryable.ToString();
                    context.Result = await queryable.ToListAsync(context.RequestAborted);
                }
            });

        if (withPaging)
        {
            field.UsePaging<ObjectType<TEntity>>();
        }

        field.UseFiltering<TType>();
    }

    private Func<IResolverContext, IRavenQueryable<TResult>> BuildResolver<TResult>(
        IDocumentStore store,
        params TResult[] results)
        where TResult : class
    {
        using var session = store.OpenSession();

        foreach (var item in results)
        {
            session.Store(item);
        }

        session.SaveChanges();

        return ctx => ((IAsyncDocumentSession)ctx.LocalContextData["session"]!).Query<TResult>();
    }
}
