using System.Diagnostics.CodeAnalysis;
using HotChocolate.Exporters.OpenApi;
using HotChocolate.Fusion.Configuration;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class FusionGatewayBuilderExtensions
{
    public static IFusionGatewayBuilder AddOpenApiDefinitionStorage(
        this IFusionGatewayBuilder builder,
        IOpenApiDefinitionStorage storage)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(storage);

        builder.AddOpenApiDocumentStorageCore();

        return builder.ConfigureSchemaServices((_, s) => s.AddSingleton(storage));
    }

    public static IFusionGatewayBuilder AddOpenApiDefinitionStorage<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IFusionGatewayBuilder builder)
        where T : class, IOpenApiDefinitionStorage
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddOpenApiDocumentStorageCore();

        return builder.ConfigureSchemaServices(
            static (_, s) => s.AddSingleton<IOpenApiDefinitionStorage, T>());
    }

    private static void AddOpenApiDocumentStorageCore(this IFusionGatewayBuilder builder)
    {
        var schemaName = builder.Name;

        builder.Services.AddOpenApiExporterServices(schemaName);

        builder.ConfigureSchemaServices((applicationServices, services)
            => services.AddOpenApiExporterSchemaServices(schemaName, applicationServices));

        builder.AddWarmupTask<OpenApiWarmupTask>();
    }
}
