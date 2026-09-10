using HotChocolate.Buffers;
using HotChocolate.Features;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Planning;
using HotChocolate.Language;

namespace Microsoft.Extensions.DependencyInjection;

// TODO [17]: Move the shared implementations to the router extensions and remove this legacy surface.
[Obsolete("Use CoreFusionRouterBuilderExtensions instead.")]
public static partial class CoreFusionGatewayBuilderExtensions
{
    [Obsolete("Use ConfigureSchemaFeatures on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder ConfigureSchemaFeatures(
        this IFusionGatewayBuilder builder,
        Action<IServiceProvider, IFeatureCollection> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        return FusionSetupUtilities.Configure(
            builder,
            setup => setup.SchemaFeaturesModifiers.Add(configure));
    }

    [Obsolete("Use ConfigureSchemaServices on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder ConfigureSchemaServices(
        this IFusionGatewayBuilder builder,
        Action<IServiceProvider, IServiceCollection> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        return FusionSetupUtilities.Configure(
            builder,
            setup => setup.SchemaServiceModifiers.Add(configure));
    }

    [Obsolete("Use AddConfigurationProvider on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder AddConfigurationProvider(
        this IFusionGatewayBuilder builder,
        Func<IServiceProvider, IFusionConfigurationProvider> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        return FusionSetupUtilities.Configure(
            builder,
            setup => setup.DocumentProvider = configure);
    }

    [Obsolete("Use AddFileSystemConfiguration on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder AddFileSystemConfiguration(
        this IFusionGatewayBuilder builder,
        string fileName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(fileName);

        return FusionSetupUtilities.Configure(
            builder,
            setup => setup.DocumentProvider =
                _ => new FileSystemFusionConfigurationProvider(
                    fileName));
    }

    [Obsolete("Use AddInMemoryConfiguration on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder AddInMemoryConfiguration(
        this IFusionGatewayBuilder builder,
        DocumentNode schemaDocument,
        JsonDocumentOwner? schemaSettings = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(schemaDocument);

        return FusionSetupUtilities.Configure(
            builder,
            setup => setup.DocumentProvider =
                _ => new InMemoryFusionConfigurationProvider(
                    schemaDocument,
                    schemaSettings));
    }

    [Obsolete("Use AddOperationPlannerInterceptor on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder AddOperationPlannerInterceptor(
        this IFusionGatewayBuilder builder,
        Func<IServiceProvider, IOperationPlannerInterceptor> factory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);

        builder.ConfigureSchemaServices((_, sc) => sc.AddSingleton(factory));
        return builder;
    }
}
