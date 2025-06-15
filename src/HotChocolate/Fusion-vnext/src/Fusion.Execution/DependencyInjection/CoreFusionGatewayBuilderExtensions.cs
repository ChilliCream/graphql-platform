using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class CoreFusionGatewayBuilderExtensions
{
    private static IFusionGatewayBuilder Configure(
        IFusionGatewayBuilder builder,
        Action<FusionGatewaySetup> configure)
    {
        builder.Services.Configure(configure);
        return builder;
    }

    public static IFusionGatewayBuilder ConfigureSchemaServices(
        this IFusionGatewayBuilder builder,
        Action<IServiceProvider, IServiceCollection> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        return Configure(
            builder,
            setup => setup.SchemaServiceModifiers.Add(configure));
    }

    public static IFusionGatewayBuilder AddFileSystemConfiguration(
        this IFusionGatewayBuilder builder,
        string fileName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(fileName);

        return Configure(
            builder,
            setup => setup.DocumentProvider = _ => new FileSystemFusionConfigurationProvider(fileName));
    }
}
