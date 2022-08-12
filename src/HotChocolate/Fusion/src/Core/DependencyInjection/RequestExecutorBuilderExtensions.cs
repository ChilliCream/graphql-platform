using HotChocolate.Execution.Configuration;
using HotChocolate.Fusion.Clients;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Fusion.Pipeline;
using HotChocolate.Fusion.Planning;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class RequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddGraphQLGateway(
        this IRequestExecutorBuilder builder,
        string serviceConfig,
        string sdl)
    {
        var configuration = ServiceConfiguration.Load(serviceConfig);

        return builder
            .AddDocumentFromString(sdl)
            .UseField(next => next)
            .UseDefaultGatewayPipeline()
            .ConfigureSchemaServices(
                sc =>
                {
                    foreach (var schemaName in configuration.Bindings)
                    {
                        sc.AddSingleton<IGraphQLClient>(
                            sp => new GraphQLHttpClient(
                                schemaName,
                                sp.GetApplicationService<IHttpClientFactory>()));
                    }

                    sc.TryAddSingleton(configuration);
                    sc.TryAddSingleton<RequestPlaner>();
                    sc.TryAddSingleton<RequirementsPlaner>();
                    sc.TryAddSingleton<ExecutionPlanBuilder>();
                    sc.TryAddSingleton<GraphQLClientFactory>();
                    sc.TryAddSingleton<FederatedQueryExecutor>();
                });
    }

    public static IRequestExecutorBuilder UseDefaultGatewayPipeline(
        this IRequestExecutorBuilder builder)
    {
        return builder
            .UseInstrumentations()
            .UseExceptions()
            .UseTimeout()
            .UseDocumentCache()
            .UseDocumentParser()
            .UseDocumentValidation()
            .UseOperationCache()
            .UseOperationComplexityAnalyzer()
            .UseOperationResolver()
            .UseOperationVariableCoercion()
            .UseRequest<QueryPlanMiddleware>()
            .UseRequest<OperationExecutionMiddleware>();
    }
}
