using HotChocolate.Data.Projections;
using HotChocolate.Data.Raven.Projections;
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

public class ProjectionVisitorTestBase : IAsyncLifetime
{
    public RavenDBResource<CustomRavenDBDefaultOptions> Resource { get; } = new();

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

    protected IRequestExecutor CreateSchema<TEntity>(
        TEntity[] entities,
        ProjectionProvider? provider = null,
        bool usePaging = false,
        bool useOffsetPaging = false,
        INamedType? objectType = null,
        Action<IRequestExecutorBuilder>? configure = null,
        Type? schemaType = null)
        where TEntity : class
    {
        provider ??= new RavenQueryableProjectionProvider();
        var convention = new ProjectionConvention(x => x.Provider(provider));

        var dbName = $"DB_{Guid.NewGuid():N}";
        var documentStore = Resource.CreateDatabase(dbName);

        var resolver = BuildResolver(documentStore, entities);

        var builder = new ServiceCollection()
            .AddSingleton(documentStore)
            .AddGraphQLServer();

        if (objectType is not null)
        {
            builder.AddType(objectType);
        }

        configure?.Invoke(builder);

        builder
            .AddConvention<IProjectionConvention>(convention)
            .AddRavenFiltering()
            .AddRavenProjections()
            .AddRavenSorting()
            .AddProjections()
            .AddQueryType(
                new ObjectType<StubObject<TEntity>>(
                    c =>
                    {
                        c.Name("Query");

                        ApplyConfigurationToFieldDescriptor<TEntity>(
                            documentStore,
                            c.Field(x => x.Root).Resolve(resolver),
                            schemaType,
                            usePaging,
                            useOffsetPaging);

                        ApplyConfigurationToFieldDescriptor<TEntity>(
                            documentStore,
                            c.Field("rootExecutable")
                                .Resolve(ctx => resolver(ctx).AsExecutable()),
                            schemaType,
                            usePaging,
                            useOffsetPaging);
                    }))
            .ModifyOptions(o => o.ValidatePipelineOrder = false)
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
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
            .UseDefaultPipeline();

        return builder.BuildRequestExecutorAsync().GetAwaiter().GetResult();
    }

    private static void ApplyConfigurationToFieldDescriptor<TEntity>(
        IDocumentStore store,
        IObjectFieldDescriptor descriptor,
        Type? type,
        bool usePaging = false,
        bool useOffsetPaging = false)
    {
        descriptor.Use(
            next => async context =>
            {
                using (var session = store.OpenAsyncSession())
                {
                    context.LocalContextData = context.LocalContextData.SetItem("session", session);
                    await next(context);
                }
            });
        if (usePaging)
        {
            descriptor.UsePaging(nodeType: type ?? typeof(ObjectType<TEntity>));
        }

        if (useOffsetPaging)
        {
            descriptor.UseOffsetPaging(type ?? typeof(ObjectType<TEntity>));
        }

        descriptor
            .Use(
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
                })
            .UseProjection()
            .UseFiltering()
            .UseSorting();
    }

    public class StubObject<T>
    {
        public T? Root { get; set; }
    }
}
