using HotChocolate;
using HotChocolate.Execution.Configuration;
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
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> that can be used to configure the Gateway.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services"/> is <c>null</c> or
    /// <paramref name="fusionGraphDocument"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder AddFusionGatewayServer(
        this IServiceCollection services,
        DocumentNode fusionGraphDocument)
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
            _ => new ValueTask<DocumentNode>(fusionGraphDocument));
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
    public static IRequestExecutorBuilder AddFusionGatewayServer(
        this IServiceCollection services,
        string fusionGraphFile,
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

        var builder = services.AddFusionGatewayServer(ct => LoadDocumentAsync(fusionGraphFile, ct));

        if (watchFileForUpdates)
        {
            builder.AddTypeModule(_ => new FileWatcherTypeModule(fusionGraphFile));
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
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> that can be used to configure the Gateway.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services"/> is <c>null</c> or
    /// <paramref name="fusionGraphResolver"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder AddFusionGatewayServer(
        this IServiceCollection services,
        ResolveFusionGraphDocAsync fusionGraphResolver)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (fusionGraphResolver is null)
        {
            throw new ArgumentNullException(nameof(fusionGraphResolver));
        }

        return services
            .AddGraphQLServer()
            .UseField(next => next)
            .UseDefaultGatewayPipeline()
            .AddOperationCompilerOptimizer<OperationQueryPlanCompiler>()
            .AddOperationCompilerOptimizer<FieldFlagsOptimizer>()
            .Configure(
                c =>
                {
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
    }

    private static IRequestExecutorBuilder UseDefaultGatewayPipeline(
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
            .UseRequest<OperationExecutionMiddleware>();
    }

    private static GraphQLClientFactory CreateGraphQLClientFactory(
        IServiceProvider sp,
        FusionGraphConfiguration fusionGraphConfig)
    {
        var appSp = sp.GetApplicationServices();
        var clientFactory = appSp.GetRequiredService<IHttpClientFactory>();
        var map1 = new Dictionary<string, Func<IGraphQLClient>>();
        var map2 = new Dictionary<string, Func<IGraphQLSubscriptionClient>>();

        IGraphQLClient CreateClient(
            HttpClientConfiguration clientConfig)
            => new HttpGraphQLClient(
                clientConfig,
                clientFactory.CreateClient(clientConfig.ClientName));

        foreach (var config in fusionGraphConfig.HttpClients)
        {
            map1.Add(config.SubgraphName, () => CreateClient(config));
        }

        var subClientFactory =
            appSp.GetService<IWebSocketConnectionFactory>();

        if (subClientFactory is not null)
        {
            IGraphQLSubscriptionClient Create(
                WebSocketClientConfiguration clientConfig)
                => new WebSocketGraphQLSubscriptionClient(
                    clientConfig,
                    subClientFactory.CreateConnection(
                        clientConfig.ClientName));


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
