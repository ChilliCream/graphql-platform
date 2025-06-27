using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Clients;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class CoreFusionGatewayBuilderExtensions
{
    /// <summary>
    /// Adds a http client configuration to the fusion gateway.
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
    /// <param name="onBeforeSend">
    /// The action to call before the request is sent.
    /// </param>
    /// <param name="onAfterReceive">
    /// The action to call after the response is received.
    /// </param>
    /// <returns>
    /// The fusion gateway builder.
    /// </returns>
    public static IFusionGatewayBuilder AddHttpClientConfiguration(
        this IFusionGatewayBuilder builder,
        string name,
        Uri baseAddress,
        SupportedOperationType supportedOperations = SupportedOperationType.All,
        Action<OperationPlanContext, HttpRequestMessage>? onBeforeSend = null,
        Action<OperationPlanContext, HttpResponseMessage>? onAfterReceive = null)
        => AddHttpClientConfiguration(
            builder,
            name,
            name,
            baseAddress,
            supportedOperations,
            onBeforeSend,
            onAfterReceive);

    /// <summary>
    /// Adds a http client configuration to the fusion gateway.
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
    /// <param name="onBeforeSend">
    /// The action to call before the request is sent.
    /// </param>
    /// <param name="onAfterReceive">
    /// The action to call after the response is received.
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
        Action<OperationPlanContext, HttpRequestMessage>? onBeforeSend = null,
        Action<OperationPlanContext, HttpResponseMessage>? onAfterReceive = null)
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
                onBeforeSend,
                onAfterReceive));
    }

    /// <summary>
    /// Adds a http client configuration to the fusion gateway.
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
    /// Adds a http client configuration to the fusion gateway.
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

        return Configure(
            builder,
            setup => setup.ClientConfigurationModifiers.Add(create));
    }
}
