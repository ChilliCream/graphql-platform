using HotChocolate.Buffers;
using HotChocolate.Features;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Planning;
using HotChocolate.Language;

namespace Microsoft.Extensions.DependencyInjection;

// The gateway entry points hold the shared operations until the compatibility surface is removed.
// They operate on the common builder contract, including custom legacy-only implementations.
#pragma warning disable CS0618

/// <summary>
/// Provides methods for configuring a Fusion router.
/// </summary>
public static partial class CoreFusionRouterBuilderExtensions
{
    /// <summary>
    /// Registers a callback to configure the router schema features.
    /// </summary>
    public static IFusionRouterBuilder ConfigureSchemaFeatures(
        this IFusionRouterBuilder builder,
        Action<IServiceProvider, IFeatureCollection> configure)
    {
        CoreFusionGatewayBuilderExtensions.ConfigureSchemaFeatures(builder, configure);
        return builder;
    }

    /// <summary>
    /// Registers a callback to configure services for the router schema.
    /// </summary>
    public static IFusionRouterBuilder ConfigureSchemaServices(
        this IFusionRouterBuilder builder,
        Action<IServiceProvider, IServiceCollection> configure)
    {
        CoreFusionGatewayBuilderExtensions.ConfigureSchemaServices(builder, configure);
        return builder;
    }

    /// <summary>
    /// Registers the configuration provider for the router schema.
    /// </summary>
    public static IFusionRouterBuilder AddConfigurationProvider(
        this IFusionRouterBuilder builder,
        Func<IServiceProvider, IFusionConfigurationProvider> configure)
    {
        CoreFusionGatewayBuilderExtensions.AddConfigurationProvider(builder, configure);
        return builder;
    }

    /// <summary>
    /// Loads the router configuration from a file and watches it for changes.
    /// </summary>
    public static IFusionRouterBuilder AddFileSystemConfiguration(
        this IFusionRouterBuilder builder,
        string fileName)
    {
        CoreFusionGatewayBuilderExtensions.AddFileSystemConfiguration(builder, fileName);
        return builder;
    }

    /// <summary>
    /// Loads the router configuration from an in-memory schema and settings.
    /// </summary>
    public static IFusionRouterBuilder AddInMemoryConfiguration(
        this IFusionRouterBuilder builder,
        DocumentNode schemaDocument,
        JsonDocumentOwner? schemaSettings = null)
    {
        CoreFusionGatewayBuilderExtensions.AddInMemoryConfiguration(builder, schemaDocument, schemaSettings);
        return builder;
    }

    /// <summary>
    /// Adds an operation planner interceptor to the router schema services.
    /// </summary>
    public static IFusionRouterBuilder AddOperationPlannerInterceptor(
        this IFusionRouterBuilder builder,
        Func<IServiceProvider, IOperationPlannerInterceptor> factory)
    {
        CoreFusionGatewayBuilderExtensions.AddOperationPlannerInterceptor(builder, factory);
        return builder;
    }
}
