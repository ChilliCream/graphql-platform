using System.Diagnostics.CodeAnalysis;
using HotChocolate.Adapters.OpenApi;
using HotChocolate.Fusion.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

        builder.Services.AddKeyedSingleton(builder.Name, storage);

        return builder;
    }

    public static IFusionGatewayBuilder AddOpenApiDefinitionStorage<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IFusionGatewayBuilder builder)
        where T : class, IOpenApiDefinitionStorage
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddOpenApiDocumentStorageCore();

        builder.Services.AddKeyedSingleton<IOpenApiDefinitionStorage, T>(builder.Name);

        return builder;
    }

    private static void AddOpenApiDocumentStorageCore(this IFusionGatewayBuilder builder)
    {
        var schemaName = builder.Name;

        builder.Services.AddOpenApiExporterServices(schemaName);

        builder.ConfigureSchemaServices((applicationServices, services) =>
        {
            services.TryAddSingleton<IOpenApiResultFormatter, FusionOpenApiResultFormatter>();
            services.AddOpenApiExporterSchemaServices(schemaName, applicationServices);
        });

        builder.AddWarmupTask<OpenApiWarmupTask>();
    }
}
