using HotChocolate.Buffers;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Language;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class CoreFusionGatewayBuilderExtensions
{
    internal static IFusionGatewayBuilder Configure(
        IFusionGatewayBuilder builder,
        Action<FusionGatewaySetup> configure)
    {
        builder.Services.Configure(builder.Name, configure);
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

    public static IFusionGatewayBuilder AddInMemoryConfiguration(
        this IFusionGatewayBuilder builder,
        DocumentNode schemaDocument,
        JsonDocumentOwner? schemaSettings = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(schemaDocument);

        return Configure(
            builder,
            setup => setup.DocumentProvider = _ => new InMemoryFusionConfigurationProvider(schemaDocument, schemaSettings));
    }
}
