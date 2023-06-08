using System.Net;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Pipeline;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Clients;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Fusion.Pipeline;
using HotChocolate.Fusion.Planning;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection.Extensions;
using static HotChocolate.Fusion.Metadata.FusionGraphConfiguration;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class FusionRequestExecutorBuilderExtensions
{
    /// <summary>
    /// Adds a Fusion GraphQL Gateway to the service collection.
    /// </summary>
    /// <param name="services">
    /// The service collection.
    /// </param>
    /// <param name="fusionGraphDocument">
    /// The fusion graph document.
    /// </param>
    /// <param name="graphName">
    /// The name of the fusion graph.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> that can be used to configure the Gateway.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services"/> is <c>null</c> or
    /// <paramref name="fusionGraphDocument"/> is <c>null</c>.
    /// </exception>
    public static FusionGatewayBuilder AddFusionGatewayServer(
        this IServiceCollection services,
        DocumentNode fusionGraphDocument,
        string? graphName = default)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (fusionGraphDocument is null)
        {
            throw new ArgumentNullException(nameof(fusionGraphDocument));
        }

        return services.AddFusionGatewayServer(
            _ => new ValueTask<DocumentNode>(fusionGraphDocument),
            graphName: graphName);
    }

    /// <summary>
    /// Adds a Fusion GraphQL Gateway to the service collection.
    /// </summary>
    /// <param name="services">
    /// The service collection.
    /// </param>
    /// <param name="fusionGraphFile">
    /// The path to the fusion graph package file or fusion graph file.
    /// </param>
    /// <param name="graphName">
    /// The name of the fusion graph.
    /// </param>
    /// <param name="watchFileForUpdates">
    /// If set to <c>true</c> the fusion graph file will be watched for updates and
    /// the schema is rebuild whenever the file changes.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> that can be used to configure the Gateway.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services"/> is <c>null</c> or
    /// <paramref name="fusionGraphFile"/> is <c>null</c> or
    /// <paramref name="fusionGraphFile"/> is equals to <see cref="string.Empty"/>.
    /// </exception>
    public static FusionGatewayBuilder AddFusionGatewayServer(
        this IServiceCollection services,
        string fusionGraphFile,
        string? graphName = default,
        bool watchFileForUpdates = false)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (string.IsNullOrEmpty(fusionGraphFile))
        {
            throw new ArgumentNullException(nameof(fusionGraphFile));
        }

        var builder = services.AddFusionGatewayServer(
            ct => LoadDocumentAsync(fusionGraphFile, ct),
            graphName: graphName);

        if (watchFileForUpdates)
        {
            builder.CoreBuilder.AddTypeModule(_ => new FileWatcherTypeModule(fusionGraphFile));
        }

        return builder;
    }

    /// <summary>
    /// Adds a Fusion GraphQL Gateway to the service collection.
    /// </summary>
    /// <param name="services">
    /// The service collection.
    /// </param>
    /// <param name="fusionGraphResolver">
    /// A delegate that is used to resolve a fusion graph document.
    /// </param>
    /// <param name="graphName">
    /// The name of the fusion graph.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> that can be used to configure the Gateway.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services"/> is <c>null</c> or
    /// <paramref name="fusionGraphResolver"/> is <c>null</c>.
    /// </exception>
    public static FusionGatewayBuilder AddFusionGatewayServer(
        this IServiceCollection services,
        ResolveFusionGraphDocAsync fusionGraphResolver,
        string? graphName = default)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (fusionGraphResolver is null)
        {
            throw new ArgumentNullException(nameof(fusionGraphResolver));
        }

        services.AddTransient<IWebSocketConnectionFactory>(
            _ => new DefaultWebSocketConnectionFactory());
        services.TryAddSingleton<IGraphQLClientFactory>(
            sp => new DefaultHttpGraphQLClientFactory(
                sp.GetRequiredService<IHttpClientFactory>()));
        services.TryAddSingleton<IGraphQLSubscriptionClientFactory>(
            sp => new DefaultWebSocketGraphQLSubscriptionClientFactory(
                sp.GetRequiredService<IWebSocketConnectionFactory>()));

        var builder = services
            .AddGraphQLServer(graphName)
            .UseField(next => next)
            .AddOperationCompilerOptimizer<OperationQueryPlanCompiler>()
            .AddOperationCompilerOptimizer<FieldFlagsOptimizer>()
            .Configure(
                c =>
                {
                    c.DefaultPipelineFactory = AddDefaultPipeline;

                    c.OnConfigureRequestExecutorOptionsHooks.Add(
                        new OnConfigureRequestExecutorOptionsAction(
                            async: async (ctx, _, ct) =>
                            {
                                var rewriter = new FusionGraphConfigurationToSchemaRewriter();
                                var fusionGraphDoc = await fusionGraphResolver(ct);
                                var fusionGraphConfig = Load(fusionGraphDoc);
                                var schemaDoc = rewriter.Rewrite(fusionGraphDoc);

                                ctx.SchemaBuilder
                                    .AddDocument(schemaDoc)
                                    .SetFusionGraphConfig(fusionGraphConfig);
                            }));

                    c.OnConfigureSchemaServicesHooks.Add(
                        (ctx, sc) =>
                        {
                            var fusionGraphConfig = ctx.SchemaBuilder.GetFusionGraphConfig();
                            sc.AddSingleton<GraphQLClientFactory>(
                                sp => CreateGraphQLClientFactory(sp, fusionGraphConfig));
                            sc.TryAddSingleton(fusionGraphConfig);
                            sc.TryAddSingleton<QueryPlanner>();
                        });
                });

        return new FusionGatewayBuilder(builder);
    }

    /// <summary>
    /// Uses the default fusion gateway pipeline.
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static FusionGatewayBuilder UseDefaultPipeline(
        this FusionGatewayBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.CoreBuilder.UseFusionDefaultPipeline();
        return builder;
    }

    /// <summary>
    /// Uses the persisted query pipeline with the Fusion gateway.
    /// </summary>
    /// <param name="builder">
    /// The gateway builder.
    /// </param>
    /// <returns>
    /// Returns the gateway builder for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    public static FusionGatewayBuilder UsePersistedQueryPipeline(
        this FusionGatewayBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.CoreBuilder.UseFusionPersistedQueryPipeline();
        return builder;
    }

    /// <summary>
    /// Uses the automatic persisted query pipeline with the Fusion gateway.
    /// </summary>
    /// <param name="builder">
    /// The gateway builder.
    /// </param>
    /// <returns>
    /// Returns the gateway builder for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    public static FusionGatewayBuilder UseAutomaticPersistedQueryPipeline(
        this FusionGatewayBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.CoreBuilder.UseFusionAutomaticPersistedQueryPipeline();
        return builder;
    }

    private static IRequestExecutorBuilder UseFusionDefaultPipeline(
        this IRequestExecutorBuilder builder)
    {
        return builder
            .UseInstrumentation()
            .UseExceptions()
            .UseTimeout()
            .UseDocumentCache()
            .UseDocumentParser()
            .UseDocumentValidation()
            .UseOperationCache()
            .UseOperationComplexityAnalyzer()
            .UseOperationResolver()
            .UseOperationVariableCoercion()
            .UseDistributedOperationExecution();
    }

    private static IRequestExecutorBuilder UseFusionPersistedQueryPipeline(
        this IRequestExecutorBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder
            .UseInstrumentation()
            .UseExceptions()
            .UseTimeout()
            .UseDocumentCache()
            .UseReadPersistedQuery()
            .UsePersistedQueryNotFound()
            .UseOnlyPersistedQueriesAllowed()
            .UseDocumentParser()
            .UseDocumentValidation()
            .UseOperationCache()
            .UseOperationComplexityAnalyzer()
            .UseOperationResolver()
            .UseOperationVariableCoercion()
            .UseDistributedOperationExecution();
    }

    private static IRequestExecutorBuilder UseFusionAutomaticPersistedQueryPipeline(
        this IRequestExecutorBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder
            .UseInstrumentation()
            .UseExceptions()
            .UseTimeout()
            .UseDocumentCache()
            .UseReadPersistedQuery()
            .UseAutomaticPersistedQueryNotFound()
            .UseWritePersistedQuery()
            .UseDocumentParser()
            .UseDocumentValidation()
            .UseOperationCache()
            .UseOperationComplexityAnalyzer()
            .UseOperationResolver()
            .UseOperationVariableCoercion()
            .UseDistributedOperationExecution();
    }

    public static IRequestExecutorBuilder UseDistributedOperationExecution(
        this IRequestExecutorBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.UseRequest<DistributedOperationExecutionMiddleware>();
    }

    internal static void AddDefaultPipeline(this IList<RequestCoreMiddleware> pipeline)
    {
        pipeline.Add(RequestClassMiddlewareFactory.Create<InstrumentationMiddleware>());
        pipeline.Add(RequestClassMiddlewareFactory.Create<ExceptionMiddleware>());
        pipeline.Add(RequestClassMiddlewareFactory.Create<TimeoutMiddleware>());
        pipeline.Add(RequestClassMiddlewareFactory.Create<DocumentCacheMiddleware>());
        pipeline.Add(RequestClassMiddlewareFactory.Create<DocumentParserMiddleware>());
        pipeline.Add(RequestClassMiddlewareFactory.Create<DocumentValidationMiddleware>());
        pipeline.Add(RequestClassMiddlewareFactory.Create<OperationCacheMiddleware>());
        pipeline.Add(RequestClassMiddlewareFactory.Create<OperationComplexityMiddleware>());
        pipeline.Add(RequestClassMiddlewareFactory.Create<OperationResolverMiddleware>());
        pipeline.Add(RequestClassMiddlewareFactory.Create<OperationVariableCoercionMiddleware>());
        pipeline.Add(RequestClassMiddlewareFactory.Create<DistributedOperationExecutionMiddleware>());
    }

    private static GraphQLClientFactory CreateGraphQLClientFactory(
        IServiceProvider sp,
        FusionGraphConfiguration fusionGraphConfig)
    {
        var appSp = sp.GetApplicationServices();
        var clientFactory = appSp.GetRequiredService<IGraphQLClientFactory>();
        var map1 = new Dictionary<string, Func<IGraphQLClient>>();
        var map2 = new Dictionary<string, Func<IGraphQLSubscriptionClient>>();

        IGraphQLClient CreateClient(HttpClientConfiguration clientConfig)
            => clientFactory.CreateClient(clientConfig);

        foreach (var config in fusionGraphConfig.HttpClients)
        {
            map1.Add(config.SubgraphName, () => CreateClient(config));
        }

        var subClientFactory = appSp.GetService<IGraphQLSubscriptionClientFactory>();

        if (subClientFactory is not null)
        {
            IGraphQLSubscriptionClient Create(IGraphQLClientConfiguration clientConfig)
                => subClientFactory.CreateClient(clientConfig);

            foreach (var config in fusionGraphConfig.WebSocketClients)
            {
                map2.Add(config.SubgraphName, () => Create(config));
            }
        }

        return new GraphQLClientFactory(map1, map2);
    }

    private static async ValueTask<DocumentNode> LoadDocumentAsync(
        string fileName,
        CancellationToken cancellationToken)
    {
        try
        {
            // We first try to load the file name as a fusion graph package.
            // This might fails as a the file that was provided is a fusion
            // graph document.
            await using var package = FusionGraphPackage.Open(fileName, FileAccess.Read);
            return await package.GetFusionGraphAsync(cancellationToken);
        }
        catch
        {
            // If we fail to load the file as a fusion graph package we will
            // try to load it as a GraphQL schema document.
            var sourceText = await File.ReadAllTextAsync(fileName, cancellationToken);
            return Utf8GraphQLParser.Parse(sourceText);
        }
    }

    /// <summary>
    /// Builds a <see cref="IRequestExecutor"/> from the specified
    /// <see cref="FusionGatewayBuilder"/>.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="FusionGatewayBuilder"/>.
    /// </param>
    /// <param name="graphName">
    /// The name of the graph that shall be built.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns></returns>
    internal static ValueTask<IRequestExecutor> BuildRequestExecutorAsync(
        this FusionGatewayBuilder builder,
        string? graphName = default,
        CancellationToken cancellationToken = default)
        => builder.CoreBuilder.BuildRequestExecutorAsync(graphName, cancellationToken);

    /// <summary>
    /// Builds a <see cref="ISchema"/> from the specified <see cref="FusionGatewayBuilder"/>.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="FusionGatewayBuilder"/>.
    /// </param>
    /// <param name="graphName">
    /// The name of the graph that shall be built.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns></returns>
    internal static ValueTask<ISchema> BuildSchemaAsync(
        this FusionGatewayBuilder builder,
        string? graphName = default,
        CancellationToken cancellationToken = default)
        => builder.CoreBuilder.BuildSchemaAsync(graphName, cancellationToken);
}

static file class FileExtensions
{
    private const string _fusionGraphConfig = "HotChocolate.Fusion.FusionGraphConfig";

    public static FusionGraphConfiguration GetFusionGraphConfig(
        this ISchemaBuilder builder)
        => (FusionGraphConfiguration)builder.ContextData[_fusionGraphConfig]!;

    public static ISchemaBuilder SetFusionGraphConfig(
        this ISchemaBuilder builder,
        FusionGraphConfiguration config)
        => builder.SetContextData(_fusionGraphConfig, config);
}
