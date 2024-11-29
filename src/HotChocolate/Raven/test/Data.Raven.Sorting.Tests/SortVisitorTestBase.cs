using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;
using Squadron;

namespace HotChocolate.Data.Sorting;

public class SortVisitorTestBase : IAsyncLifetime
{
    protected RavenDBResource<CustomRavenDBDefaultOptions> Resource { get; } = new();

    public Task InitializeAsync() => Resource.InitializeAsync();

    public Task DisposeAsync() => Resource.DisposeAsync();

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

    protected T[] CreateEntity<T>(params T[] entities) => entities;

    protected IRequestExecutor CreateSchema<TEntity, T>(
        TEntity[] entities,
        SortConvention? convention = null)
        where TEntity : class
        where T : SortInputType<TEntity>
    {
        var dbName = $"DB_{Guid.NewGuid():N}";
        var documentStore = Resource.CreateDatabase(dbName);

        var resolver = BuildResolver(documentStore, entities);

        var builder = SchemaBuilder.New()
            .AddRavenSorting()
            .AddQueryType(
                c =>
                {
                    ApplyConfigurationToField<TEntity, T>(
                        documentStore,
                        c.Name("Query").Field("root").Resolve(resolver),
                        false);

                    ApplyConfigurationToField<TEntity, T>(
                        documentStore,
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
                else if (context.Result is IExecutable<TEntity> executable)
                {
                    context.ContextData["sql"] = executable.Print();
                    context.Result = await executable.ToListAsync(context.RequestAborted);
                }
            });

        if (withPaging)
        {
            field.UsePaging<ObjectType<TEntity>>();
        }

        field.UseSorting<TType>();
    }
}
