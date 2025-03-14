using HotChocolate;
using HotChocolate.CostAnalysis;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Errors;
using HotChocolate.Execution.Options;
using HotChocolate.Execution.Pipeline;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Clients;
using HotChocolate.Fusion.Execution.Diagnostic;
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
    /// <param name="disableDefaultSecurity">
    /// If set to <c>true</c> the default security policy is disabled.
    /// </param>
    /// <returns>
    /// Returns the <see cref="FusionGatewayBuilder"/> that can be used to configure the Gateway.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services"/> is <c>null</c> or
    /// </exception>
    public static FusionGatewayBuilder AddFusionGatewayServer(
        this IServiceCollection services,
        string? graphName = null,
        bool disableDefaultSecurity = false)
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
            .AddGraphQLServer(graphName, disableDefaultSecurity: disableDefaultSecurity)
            .UseField(next => next)
            .AddOperationCompilerOptimizer<OperationQueryPlanCompiler>()
            .AddOperationCompilerOptimizer<FieldFlagsOptimizer>()
            .AddOperationCompilerOptimizer<SemanticNonNullOptimizer>()
            .AddConvention<INamingConventions>(_ => new DefaultNamingConventions())
            .ModifyCostOptions(o => o.ApplyCostDefaults = false)
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

                            sc.TryAddSingleton(GetFusionOptions);
                            sc.TryAddFusionDiagnosticEvents();
                        });
                });

        return new FusionGatewayBuilder(builder);
    }

    public static FusionGatewayBuilder AddDiagnosticEventListener<T>(
        this FusionGatewayBuilder builder)
        where T : class, IFusionDiagnosticEventListener
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Services.TryAddSingleton<T>();
        builder.CoreBuilder.ConfigureSchemaServices(
            s => s.AddSingleton(
                sp => (IFusionDiagnosticEventListener)sp.GetApplicationService<T>()));

        return builder;
    }

    internal static IServiceCollection TryAddFusionDiagnosticEvents(
        this IServiceCollection services)
    {
        services.TryAddSingleton<IFusionDiagnosticEvents>(sp =>
        {
            var listeners = sp.GetServices<IFusionDiagnosticEventListener>().ToArray();
            return listeners.Length switch
            {
                0 => new NoopFusionDiagnosticEvents(),
                1 => listeners[0],
                _ => new AggregateFusionDiagnosticEvents(listeners),
            };
        });
        return services;
    }

    /// <summary>
    /// Adds a custom ID parser to the gateway.
    /// </summary>
    /// <returns>
    /// Returns the <see cref="FusionGatewayBuilder"/> that can be used to configure the Gateway.
    /// </returns>
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
                    sc.AddSingleton<NodeIdParser, T>();
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
    /// Returns the <see cref="FusionGatewayBuilder"/> that can be used to configure the Gateway.
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
    /// Returns the <see cref="FusionGatewayBuilder"/> that can be used to configure the Gateway.
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
    /// <returns>
    /// Returns the <see cref="FusionGatewayBuilder"/> that can be used to configure the Gateway.
    /// </returns>
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
    /// Rewrites the gateway configuration to use the service discovery for HTTP clients.
    /// </summary>
    /// <param name="builder">
    /// The gateway builder.
    /// </param>
    /// <returns>
    /// Returns the <see cref="FusionGatewayBuilder"/> that can be used to configure the Gateway.
    /// </returns>
    public static FusionGatewayBuilder AddServiceDiscoveryRewriter(
        this FusionGatewayBuilder builder)
    {
        builder.Services.AddSingleton<IConfigurationRewriter, ServiceDiscoveryConfigurationRewriter>();
        return builder;
    }

    /// <summary>
    /// Adds a delegate that will be used to modify the <see cref="RequestExecutorOptions"/>.
    /// </summary>
    /// <param name="builder">
    /// The gateway builder.
    /// </param>
    /// <param name="modify">
    /// A delegate that is used to modify the <see cref="RequestExecutorOptions"/>.
    /// </param>
    /// <returns>
    /// Returns the <see cref="FusionGatewayBuilder"/> that can be used to configure the Gateway.
    /// </returns>
    public static FusionGatewayBuilder ModifyRequestOptions(
        this FusionGatewayBuilder builder,
        Action<RequestExecutorOptions> modify)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (modify is null)
        {
            throw new ArgumentNullException(nameof(modify));
        }

        builder.CoreBuilder.Configure(options => options.OnConfigureRequestExecutorOptionsHooks.Add(
            new OnConfigureRequestExecutorOptionsAction(
                (_, opt) => modify(opt))));

        return builder;
    }

    /// <summary>
    /// Adds a delegate that will be used to modify the <see cref="FusionOptions"/>.
    /// </summary>
    /// <param name="builder">
    /// The gateway builder.
    /// </param>
    /// <param name="modify">
    /// A delegate that is used to modify the <see cref="FusionOptions"/>.
    /// </param>
    /// <returns>
    /// Returns the <see cref="FusionGatewayBuilder"/> that can be used to configure the Gateway.
    /// </returns>
    public static FusionGatewayBuilder ModifyFusionOptions(
        this FusionGatewayBuilder builder,
        Action<FusionOptions> modify)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (modify is null)
        {
            throw new ArgumentNullException(nameof(modify));
        }

        builder.CoreBuilder.Configure(options => options.OnConfigureSchemaServicesHooks.Add(
            (ctx, sc) => sc.AddSingleton(modify)));

        return builder;
    }

    public static FusionGatewayBuilder AddErrorFilter(
        this FusionGatewayBuilder builder,
        Func<IError, IError> errorFilter)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (errorFilter is null)
        {
            throw new ArgumentNullException(nameof(errorFilter));
        }

        builder.CoreBuilder.ConfigureSchemaServices(
            s => s.AddSingleton<IErrorFilter>(
                new FuncErrorFilterWrapper(errorFilter)));

        return builder;
    }

    public static FusionGatewayBuilder AddErrorFilter<T>(
        this FusionGatewayBuilder builder,
        Func<IServiceProvider, T> factory)
        where T : class, IErrorFilter
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (factory is null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        builder.CoreBuilder.ConfigureSchemaServices(
            s => s.AddSingleton<IErrorFilter, T>(
                sp => factory(sp.GetCombinedServices())));

        return builder;
    }

    public static FusionGatewayBuilder AddErrorFilter<T>(
        this FusionGatewayBuilder builder)
        where T : class, IErrorFilter
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Services.TryAddSingleton<T>();
        builder.CoreBuilder.ConfigureSchemaServices(
            s => s.AddSingleton<IErrorFilter, T>(
                sp => sp.GetApplicationService<T>()));

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
    /// Uses the persisted operation pipeline with the Fusion gateway.
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
    public static FusionGatewayBuilder UsePersistedOperationPipeline(
        this FusionGatewayBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.CoreBuilder.UseFusionPersistedOperationPipeline();
        return builder;
    }

    /// <summary>
    /// Uses the automatic persisted operation pipeline with the Fusion gateway.
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
    public static FusionGatewayBuilder UseAutomaticPersistedOperationPipeline(
        this FusionGatewayBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.CoreBuilder.UseFusionAutomaticPersistedOperationPipeline();
        return builder;
    }

    /// <summary>
    /// Adds a type that will be used to create a middleware for the execution pipeline.
    /// </summary>
    /// <param name="builder">
    /// The gateway builder.
    /// </param>
    /// <returns>
    /// Returns the gateway builder for configuration chaining.
    /// </returns>
    public static FusionGatewayBuilder UseRequest<TMiddleware>(
        this FusionGatewayBuilder builder)
        where TMiddleware : class
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.CoreBuilder.UseRequest<TMiddleware>();
        return builder;
    }

    /// <summary>
    /// Adds a delegate that will be used to create a middleware for the execution pipeline.
    /// </summary>
    /// <param name="builder">
    /// The gateway builder.
    /// </param>
    /// <param name="middleware">
    /// A delegate that is used to create a middleware for the execution pipeline.
    /// </param>
    /// <returns>
    /// Returns the gateway builder for configuration chaining.
    /// </returns>
    public static FusionGatewayBuilder UseRequest(
        this FusionGatewayBuilder builder,
        RequestCoreMiddleware middleware)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (middleware is null)
        {
            throw new ArgumentNullException(nameof(middleware));
        }

        builder.CoreBuilder.UseRequest(middleware);
        return builder;
    }

    /// <summary>
    /// Adds a delegate that will be used to create a middleware for the execution pipeline.
    /// </summary>
    /// <param name="builder">
    /// The gateway builder.
    /// </param>
    /// <param name="middleware">
    /// A delegate that is used to create a middleware for the execution pipeline.
    /// </param>
    /// <returns>
    /// Returns the gateway builder for configuration chaining.
    /// </returns>
    public static FusionGatewayBuilder UseRequest(
        this FusionGatewayBuilder builder,
        RequestMiddleware middleware)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (middleware is null)
        {
            throw new ArgumentNullException(nameof(middleware));
        }

        builder.CoreBuilder.UseRequest(middleware);
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
            .UseCostAnalyzer()
            .UseOperationCache()
            .UseOperationResolver()
            .UseSkipWarmupExecution()
            .UseOperationVariableCoercion()
            .UseDistributedOperationExecution();
    }

    private static IRequestExecutorBuilder UseFusionPersistedOperationPipeline(
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
            .UseReadPersistedOperation()
            .UsePersistedOperationNotFound()
            .UseOnlyPersistedOperationAllowed()
            .UseDocumentParser()
            .UseDocumentValidation()
            .UseOperationCache()
            .UseOperationResolver()
            .UseSkipWarmupExecution()
            .UseOperationVariableCoercion()
            .UseDistributedOperationExecution();
    }

    private static IRequestExecutorBuilder UseFusionAutomaticPersistedOperationPipeline(
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
            .UseReadPersistedOperation()
            .UseAutomaticPersistedOperationNotFound()
            .UseWritePersistedOperation()
            .UseDocumentParser()
            .UseDocumentValidation()
            .UseOperationCache()
            .UseOperationResolver()
            .UseSkipWarmupExecution()
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

        return builder.UseRequest(DistributedOperationExecutionMiddleware.Create());
    }

    internal static void AddDefaultPipeline(this IList<RequestCoreMiddleware> pipeline)
    {
        pipeline.Add(InstrumentationMiddleware.Create());
        pipeline.Add(ExceptionMiddleware.Create());
        pipeline.Add(TimeoutMiddleware.Create());
        pipeline.Add(DocumentCacheMiddleware.Create());
        pipeline.Add(DocumentParserMiddleware.Create());
        pipeline.Add(DocumentValidationMiddleware.Create());
        pipeline.Add(CostAnalyzerMiddleware.Create());
        pipeline.Add(OperationCacheMiddleware.Create());
        pipeline.Add(OperationResolverMiddleware.Create());
        pipeline.Add(OperationVariableCoercionMiddleware.Create());
        pipeline.Add(DistributedOperationExecutionMiddleware.Create());
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

    private static FusionOptions GetFusionOptions(IServiceProvider sp)
    {
        var configures = sp.GetServices<Action<FusionOptions>>();
        var options = new FusionOptions();

        foreach (var configure in configures)
        {
            configure(options);
        }

        return options;
    }
}
