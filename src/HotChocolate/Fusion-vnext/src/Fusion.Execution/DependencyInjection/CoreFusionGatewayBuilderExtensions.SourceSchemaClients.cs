using System.Collections.Immutable;
using System.Net.Http.Headers;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class CoreFusionGatewayBuilderExtensions
{
    /// <summary>
    /// Adds an http client configuration to the fusion gateway.
    /// </summary>
    /// <param name="builder">
    /// The fusion gateway builder.
    /// </param>
    /// <param name="name">
    /// The name of the source schema.
    /// </param>
    /// <param name="baseAddress">
    /// The base address of the source schema.
    /// </param>
    /// <param name="supportedOperations">
    /// The supported operations.
    /// </param>
    /// <param name="batchingMode">
    /// The batching mode.
    /// </param>
    /// <param name="defaultAcceptHeaderValues">
    /// The <c>Accept</c> header values sent in case of a single, non-Subscription GraphQL request.
    /// </param>
    /// <param name="batchingAcceptHeaderValues">
    /// The <c>Accept</c> header values sent in case of a batching request.
    /// </param>>
    /// <param name="subscriptionAcceptHeaderValues">
    /// The <c>Accept</c> header values sent in case of a subscription.
    /// </param>
    /// <param name="onBeforeSend">
    /// The action to call before the request is sent.
    /// </param>
    /// <param name="onAfterReceive">
    /// The action to call after the response is received.
    /// </param>
    /// <param name="onSourceSchemaResult">
    /// The action to call after a <see cref="SourceSchemaResult"/> was materialized.
    /// </param>
    /// <returns>
    /// The fusion gateway builder.
    /// </returns>
    public static IFusionGatewayBuilder AddHttpClientConfiguration(
        this IFusionGatewayBuilder builder,
        string name,
        Uri baseAddress,
        SupportedOperationType supportedOperations = SupportedOperationType.All,
        SourceSchemaHttpClientBatchingMode batchingMode = SourceSchemaHttpClientBatchingMode.VariableBatching,
        ImmutableArray<MediaTypeWithQualityHeaderValue>? defaultAcceptHeaderValues = null,
        ImmutableArray<MediaTypeWithQualityHeaderValue>? batchingAcceptHeaderValues = null,
        ImmutableArray<MediaTypeWithQualityHeaderValue>? subscriptionAcceptHeaderValues = null,
        Action<OperationPlanContext, ExecutionNode, HttpRequestMessage>? onBeforeSend = null,
        Action<OperationPlanContext, ExecutionNode, HttpResponseMessage>? onAfterReceive = null,
        Action<OperationPlanContext, ExecutionNode, SourceSchemaResult>? onSourceSchemaResult = null)
        => AddHttpClientConfiguration(
            builder,
            name,
            name,
            baseAddress,
            supportedOperations,
            batchingMode,
            defaultAcceptHeaderValues,
            batchingAcceptHeaderValues,
            subscriptionAcceptHeaderValues,
            onBeforeSend,
            onAfterReceive,
            onSourceSchemaResult);

    /// <summary>
    /// Adds an http client configuration to the fusion gateway.
    /// </summary>
    /// <param name="builder">
    /// The fusion gateway builder.
    /// </param>
    /// <param name="name">
    /// The name of the source schema.
    /// </param>
    /// <param name="httpClientName">
    /// The name of the http client.
    /// </param>
    /// <param name="baseAddress">
    /// The base address of the source schema.
    /// </param>
    /// <param name="supportedOperations">
    /// The supported operations.
    /// </param>
    /// <param name="batchingMode">
    /// The batching mode.
    /// </param>
    /// <param name="defaultAcceptHeaderValues">
    /// The <c>Accept</c> header values sent in case of a single, non-Subscription GraphQL request.
    /// </param>
    /// <param name="batchingAcceptHeaderValues">
    /// The <c>Accept</c> header values sent in case of a batching request.
    /// </param>>
    /// <param name="subscriptionAcceptHeaderValues">
    /// The <c>Accept</c> header values sent in case of a subscription.
    /// </param>
    /// <param name="onBeforeSend">
    /// The action to call before the request is sent.
    /// </param>
    /// <param name="onAfterReceive">
    /// The action to call after the response is received.
    /// </param>
    /// <param name="onSourceSchemaResult">
    /// The action to call after a <see cref="SourceSchemaResult"/> was materialized.
    /// </param>
    /// <returns>
    /// The fusion gateway builder.
    /// </returns>
    public static IFusionGatewayBuilder AddHttpClientConfiguration(
        this IFusionGatewayBuilder builder,
        string name,
        string httpClientName,
        Uri baseAddress,
        SupportedOperationType supportedOperations = SupportedOperationType.All,
        SourceSchemaHttpClientBatchingMode batchingMode = SourceSchemaHttpClientBatchingMode.VariableBatching,
        ImmutableArray<MediaTypeWithQualityHeaderValue>? defaultAcceptHeaderValues = null,
        ImmutableArray<MediaTypeWithQualityHeaderValue>? batchingAcceptHeaderValues = null,
        ImmutableArray<MediaTypeWithQualityHeaderValue>? subscriptionAcceptHeaderValues = null,
        Action<OperationPlanContext, ExecutionNode, HttpRequestMessage>? onBeforeSend = null,
        Action<OperationPlanContext, ExecutionNode, HttpResponseMessage>? onAfterReceive = null,
        Action<OperationPlanContext, ExecutionNode, SourceSchemaResult>? onSourceSchemaResult = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(httpClientName);
        ArgumentNullException.ThrowIfNull(baseAddress);

        return AddHttpClientConfiguration(
            builder,
            new SourceSchemaHttpClientConfiguration(
                name,
                httpClientName,
                baseAddress,
                supportedOperations,
                batchingMode,
                defaultAcceptHeaderValues,
                batchingAcceptHeaderValues,
                subscriptionAcceptHeaderValues,
                onBeforeSend,
                onAfterReceive,
                onSourceSchemaResult));
    }

    /// <summary>
    /// Adds an http client configuration to the fusion gateway.
    /// </summary>
    /// <param name="builder">
    /// The fusion gateway builder.
    /// </param>
    /// <param name="configuration">
    /// The http client configuration.
    /// </param>
    /// <returns>
    /// The fusion gateway builder.
    /// </returns>
    public static IFusionGatewayBuilder AddHttpClientConfiguration(
        this IFusionGatewayBuilder builder,
        SourceSchemaHttpClientConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        return AddHttpClientConfiguration(builder, _ => configuration);
    }

    /// <summary>
    /// Adds an http client configuration to the fusion gateway.
    /// </summary>
    /// <param name="builder">
    /// The fusion gateway builder.
    /// </param>
    /// <param name="create">
    /// A function that creates the http client configuration.
    /// </param>
    /// <returns>
    /// The fusion gateway builder.
    /// </returns>
    public static IFusionGatewayBuilder AddHttpClientConfiguration(
        this IFusionGatewayBuilder builder,
        Func<IServiceProvider, SourceSchemaHttpClientConfiguration> create)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(create);

        return FusionSetupUtilities.Configure(
            builder,
            setup => setup.ClientConfigurationModifiers.Add(create));
    }
}
