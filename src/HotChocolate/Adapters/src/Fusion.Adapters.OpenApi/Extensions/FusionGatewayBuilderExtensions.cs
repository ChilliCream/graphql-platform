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

        builder.AddOpenApiDefinitionStorageCore();

        builder.ConfigureSchemaServices((_, services) => services.AddSingleton(storage));

        return builder;
    }

    public static IFusionGatewayBuilder AddOpenApiDefinitionStorage<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IFusionGatewayBuilder builder)
        where T : class, IOpenApiDefinitionStorage
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddOpenApiDefinitionStorageCore();

        builder.ConfigureSchemaServices((_, services) => services.AddSingleton<IOpenApiDefinitionStorage, T>());

        return builder;
    }

    public static IFusionGatewayBuilder AddOpenApiDefinitionStorage<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IFusionGatewayBuilder builder,
        Func<IServiceProvider, T> factory)
        where T : class, IOpenApiDefinitionStorage
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);

        builder.AddOpenApiDefinitionStorageCore();

        builder.ConfigureSchemaServices((_, services) => services.AddSingleton<IOpenApiDefinitionStorage, T>(factory));

        return builder;
    }

    private static void AddOpenApiDefinitionStorageCore(this IFusionGatewayBuilder builder)
    {
        var schemaName = builder.Name;

        builder.Services.AddOpenApiServices(schemaName);
        builder.Services.AddOpenApiAspNetCoreServices(schemaName);

        builder.ConfigureSchemaServices((applicationServices, services) =>
        {
            services.TryAddSingleton<IOpenApiResultFormatter, FusionOpenApiResultFormatter>();
            services.AddOpenApiSchemaServices(schemaName, applicationServices);
        });

        builder.AddWarmupTask<OpenApiWarmupTask>();
    }
}
