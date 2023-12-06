using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Pipeline;
using HotChocolate.Fusion.Clients;
using HotChocolate.Fusion.Execution.Pipeline;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Utilities;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.DependencyInjection.Extensions;
using static HotChocolate.Fusion.Utilities.ThrowHelper;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class FusionRequestExecutorBuilderExtensions
{
    /// <summary>
    /// Adds a Fusion GraphQL Gateway to the service collection.
    /// </summary>
    /// <param name="services">
    /// The service collection.1
    /// </param>
    /// <param name="graphName">
    /// The name of the fusion graph.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> that can be used to configure the Gateway.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services"/> is <c>null</c> or
    /// </exception>
    public static FusionGatewayBuilder AddFusionGatewayServer(
        this IServiceCollection services,
        string? graphName = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IWebSocketConnectionFactory>(
            _ => new DefaultWebSocketConnectionFactory());
        services.TryAddSingleton<IGraphQLClientFactory>(
            sp => new DefaultHttpGraphQLClientFactory(
                sp.GetRequiredService<IHttpClientFactory>()));
        services.TryAddSingleton<IGraphQLSubscriptionClientFactory>(
            sp => new DefaultWebSocketGraphQLSubscriptionClientFactory(
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<IWebSocketConnectionFactory>()));

        var builder = services
            .AddGraphQLServer(graphName)
            .UseField(next => next)
            .AddOperationCompilerOptimizer<OperationQueryPlanCompiler>()
            .AddOperationCompilerOptimizer<FieldFlagsOptimizer>()
            .AddConvention<INamingConventions>(_ => new DefaultNamingConventions())
            .Configure(
                c =>
                {
                    c.DefaultPipelineFactory = AddDefaultPipeline;

                    c.OnConfigureSchemaServicesHooks.Add(
                        (ctx, sc) =>
                        {
                            if (!ctx.SchemaBuilder.ContainsFusionGraphConfig())
                            {
                                throw NoConfigurationProvider();
                            }

                            var fusionGraphConfig = ctx.SchemaBuilder.GetFusionGraphConfig();
                            sc.AddSingleton<GraphQLClientFactory>(
                                sp => CreateGraphQLClientFactory(sp, fusionGraphConfig));
                            sc.TryAddSingleton(fusionGraphConfig);
                            sc.TryAddSingleton<QueryPlanner>();
                            sc.TryAddSingleton<NodeIdParser, DefaultNodeIdParser>();
                        });
                });

        return new FusionGatewayBuilder(builder);
    }
    
    /// <summary>
    /// Adds a custom ID parser to the gateway.
    /// </summary>
    public static FusionGatewayBuilder AddNodeIdParser<T>(
        this FusionGatewayBuilder builder)
        where T : NodeIdParser
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.CoreBuilder.Configure(
            c => c.OnConfigureSchemaServicesHooks.Add(
                (_, sc) =>
                {
                    sc.RemoveAll<NodeIdParser>();
                    sc.AddSingleton<NodeIdParser, DefaultNodeIdParser>();
                }));
        
        return builder;
    }

    /// <summary>
    /// Specifies that the gateway configuration is loaded from a file.
    /// </summary>
    /// <param name="builder">
    /// The gateway builder.
    /// </param>
    /// <param name="gatewayConfigurationFile">
    /// The path to the fusion gateway configuration file.
    /// </param>
    /// <param name="watchFileForUpdates">
    /// If set to <c>true</c> the fusion graph file will be watched for updates and
    /// the schema is rebuild whenever the file changes.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> that can be used to configure the Gateway.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <c>null</c> or
    /// <paramref name="gatewayConfigurationFile"/> is <c>null</c> or
    /// <paramref name="gatewayConfigurationFile"/> is equals to <see cref="string.Empty"/>.
    /// </exception>
    public static FusionGatewayBuilder ConfigureFromFile(
        this FusionGatewayBuilder builder,
        string gatewayConfigurationFile,
        bool watchFileForUpdates = true)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (string.IsNullOrEmpty(gatewayConfigurationFile))
        {
            throw new ArgumentNullException(nameof(gatewayConfigurationFile));
        }

        if (watchFileForUpdates)
        {
            builder.RegisterGatewayConfiguration(
                _ => new GatewayConfigurationFileObserver(gatewayConfigurationFile));
        }
        else
        {
            builder.RegisterGatewayConfiguration(
                _ => new StaticGatewayConfigurationFileObserver(gatewayConfigurationFile));
        }

        return builder;
    }

    /// <summary>
    /// Specifies that the gateway configuration is loaded from an in-memory GraphQL document.
    /// </summary>
    /// <param name="builder">
    /// The gateway builder.
    /// </param>
    /// <param name="gatewayConfigurationDoc">
    /// The fusion gateway configuration document.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> that can be used to configure the Gateway.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <c>null</c> or
    /// <paramref name="gatewayConfigurationDoc"/> is <c>null</c>.
    /// </exception>
    public static FusionGatewayBuilder ConfigureFromDocument(
        this FusionGatewayBuilder builder,
        DocumentNode gatewayConfigurationDoc)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (gatewayConfigurationDoc is null)
        {
            throw new ArgumentNullException(nameof(gatewayConfigurationDoc));
        }

        return builder
            .RegisterGatewayConfiguration(
                _ => new StaticGatewayConfigurationObserver(gatewayConfigurationDoc));
    }

    /// <summary>
    /// Registers an observable Gateway configuration.
    /// </summary>
    /// <param name="builder">
    /// The gateway builder.
    /// </param>
    /// <param name="factory">
    /// The factory that creates the observable Gateway configuration.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <c>null</c> or
    /// <paramref name="factory"/> is <c>null</c>.
    /// </exception>
    public static FusionGatewayBuilder RegisterGatewayConfiguration(
        this FusionGatewayBuilder builder,
        Func<IServiceProvider, IObservable<GatewayConfiguration>> factory)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (factory is null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        builder.Services.AddSingleton(factory);
        builder.Services.AddSingleton<GatewayConfigurationTypeModule>();
        builder.CoreBuilder.AddTypeModule<GatewayConfigurationTypeModule>();
        return builder;
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

            foreach (var config in fusionGraphConfig.HttpClients)
            {
                if (!map2.ContainsKey(config.SubgraphName))
                {
                    map2.Add(config.SubgraphName, () => Create(config));
                }
            }
        }

        return new GraphQLClientFactory(map1, map2);
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
