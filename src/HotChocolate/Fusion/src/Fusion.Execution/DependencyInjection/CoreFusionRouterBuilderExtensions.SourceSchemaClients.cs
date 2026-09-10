using System.Collections.Immutable;
using System.Net.Http.Headers;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;

namespace Microsoft.Extensions.DependencyInjection;

#pragma warning disable CS0618 // Both builder surfaces share the same configuration operations.

public static partial class CoreFusionRouterBuilderExtensions
{
    /// <summary>
    /// Adds an HTTP source schema client configuration using the source schema name as the client name.
    /// </summary>
    public static IFusionRouterBuilder AddHttpClientConfiguration(
        this IFusionRouterBuilder builder,
        string name,
        Uri baseAddress,
        SupportedOperationType supportedOperations = SupportedOperationType.All,
        SourceSchemaClientCapabilities capabilities = SourceSchemaClientCapabilities.Default,
        ErrorHandlingMode? onError = null,
        ImmutableArray<MediaTypeWithQualityHeaderValue>? defaultAcceptHeaderValues = null,
        ImmutableArray<MediaTypeWithQualityHeaderValue>? batchingAcceptHeaderValues = null,
        ImmutableArray<MediaTypeWithQualityHeaderValue>? subscriptionAcceptHeaderValues = null,
        Action<OperationPlanContext, ExecutionNode, HttpRequestMessage>? onBeforeSend = null,
        Action<OperationPlanContext, ExecutionNode, HttpResponseMessage>? onAfterReceive = null,
        Action<OperationPlanContext, ExecutionNode, SourceSchemaResult>? onSourceSchemaResult = null)
    {
        CoreFusionGatewayBuilderExtensions.AddHttpClientConfiguration(
            builder,
            name,
            baseAddress,
            supportedOperations,
            capabilities,
            onError,
            defaultAcceptHeaderValues,
            batchingAcceptHeaderValues,
            subscriptionAcceptHeaderValues,
            onBeforeSend,
            onAfterReceive,
            onSourceSchemaResult);
        return builder;
    }

    /// <summary>
    /// Adds an HTTP source schema client configuration with an explicit HTTP client name.
    /// </summary>
    public static IFusionRouterBuilder AddHttpClientConfiguration(
        this IFusionRouterBuilder builder,
        string name,
        string httpClientName,
        Uri baseAddress,
        SupportedOperationType supportedOperations = SupportedOperationType.All,
        SourceSchemaClientCapabilities capabilities = SourceSchemaClientCapabilities.Default,
        ErrorHandlingMode? onError = null,
        ImmutableArray<MediaTypeWithQualityHeaderValue>? defaultAcceptHeaderValues = null,
        ImmutableArray<MediaTypeWithQualityHeaderValue>? batchingAcceptHeaderValues = null,
        ImmutableArray<MediaTypeWithQualityHeaderValue>? subscriptionAcceptHeaderValues = null,
        Action<OperationPlanContext, ExecutionNode, HttpRequestMessage>? onBeforeSend = null,
        Action<OperationPlanContext, ExecutionNode, HttpResponseMessage>? onAfterReceive = null,
        Action<OperationPlanContext, ExecutionNode, SourceSchemaResult>? onSourceSchemaResult = null)
    {
        CoreFusionGatewayBuilderExtensions.AddHttpClientConfiguration(
            builder,
            name,
            httpClientName,
            baseAddress,
            supportedOperations,
            capabilities,
            onError,
            defaultAcceptHeaderValues,
            batchingAcceptHeaderValues,
            subscriptionAcceptHeaderValues,
            onBeforeSend,
            onAfterReceive,
            onSourceSchemaResult);
        return builder;
    }

    /// <summary>
    /// Adds an HTTP source schema client configuration to the router.
    /// </summary>
    public static IFusionRouterBuilder AddHttpClientConfiguration(
        this IFusionRouterBuilder builder,
        HttpSourceSchemaClientConfiguration configuration)
    {
        CoreFusionGatewayBuilderExtensions.AddHttpClientConfiguration(builder, configuration);
        return builder;
    }

    /// <summary>
    /// Adds an HTTP source schema client configuration created using the application services.
    /// </summary>
    public static IFusionRouterBuilder AddHttpClientConfiguration(
        this IFusionRouterBuilder builder,
        Func<IServiceProvider, HttpSourceSchemaClientConfiguration> create)
    {
        CoreFusionGatewayBuilderExtensions.AddHttpClientConfiguration(builder, create);
        return builder;
    }
}
