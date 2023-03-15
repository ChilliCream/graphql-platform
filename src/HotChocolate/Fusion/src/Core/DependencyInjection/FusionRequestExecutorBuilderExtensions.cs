using HotChocolate.Execution.Configuration;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Clients;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Fusion.Pipeline;
using HotChocolate.Fusion.Planning;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection.Extensions;
using static HotChocolate.Fusion.FusionResources;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class FusionRequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddFusionGatewayServer(
        this IServiceCollection services,
        string serviceConfiguration)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (string.IsNullOrEmpty(serviceConfiguration))
        {
            throw new ArgumentNullException(nameof(serviceConfiguration));
        }

        var serviceConfDoc = Utf8GraphQLParser.Parse(serviceConfiguration);
        return AddFusionGatewayServer(services, serviceConfDoc);
    }

    public static IRequestExecutorBuilder AddFusionGatewayServer(
        this IServiceCollection services,
        DocumentNode serviceConfiguration)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (serviceConfiguration is null)
        {
            throw new ArgumentNullException(nameof(serviceConfiguration));
        }

        var context = FusionTypeNames.From(serviceConfiguration);
        var rewriter = new FusionGraphConfigurationToSchemaRewriter();
        var schemaDoc = (DocumentNode?)rewriter.Rewrite(serviceConfiguration, new(context));
        var configuration = FusionGraphConfiguration.Load(serviceConfiguration);

        if (schemaDoc is null)
        {
            // This should not happen as we have already validated the fusion graph configuration.
            throw new InvalidOperationException(
                FusionRequestExecutorBuilderExtensions_AddFusionGatewayServer_NoSchema);
        }

        return services
            .AddGraphQLServer()
            .AddDocument(schemaDoc)
            .UseField(next => next)
            .UseDefaultGatewayPipeline()
            .AddOperationCompilerOptimizer<OperationQueryPlanCompiler>()
            .AddOperationCompilerOptimizer<FieldFlagsOptimizer>()
            .ConfigureSchemaServices(
                sc =>
                {
                    sc.AddSingleton<GraphQLClientFactory>(
                        sp =>
                        {
                            var appSp = sp.GetApplicationServices();
                            var clientFactory = appSp.GetRequiredService<IHttpClientFactory>();
                            var map1 = new Dictionary<string, Func<IGraphQLClient>>();
                            var map2 = new Dictionary<string, Func<IGraphQLSubscriptionClient>>();

                            IGraphQLClient CreateClient(HttpClientConfiguration clientConfig)
                                => new HttpGraphQLClient(
                                    clientConfig,
                                    clientFactory.CreateClient(clientConfig.ClientName));

                            foreach (var config in configuration.HttpClients)
                            {
                                map1.Add(config.SubgraphName, () => CreateClient(config));
                            }

                            var subClientFactory = appSp.GetService<IWebSocketConnectionFactory>();
                            if (subClientFactory is not null)
                            {
                                IGraphQLSubscriptionClient Create(
                                    WebSocketClientConfiguration clientConfig)
                                    => new WebSocketGraphQLSubscriptionClient(
                                        clientConfig,
                                        subClientFactory.CreateConnection(clientConfig.ClientName));


                                foreach (var config in configuration.WebSocketClients)
                                {
                                    map2.Add(config.SubgraphName, () => Create(config));
                                }
                            }

                            return new GraphQLClientFactory(map1, map2);
                        });

                    sc.TryAddSingleton(configuration);
                    sc.TryAddSingleton<RequestPlanner>();
                    sc.TryAddSingleton<RequirementsPlanner>();
                    sc.TryAddSingleton<ExecutionPlanBuilder>();
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
}
