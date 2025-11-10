using System.Diagnostics.CodeAnalysis;
using HotChocolate.Adapters.OpenApi;
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

    public static IFusionGatewayBuilder AddOpenApiDiagnosticEventListener(
        this IFusionGatewayBuilder builder,
        IOpenApiDiagnosticEventListener listener)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(listener);

        builder.ConfigureSchemaServices((_, s) => s.AddSingleton(listener));

        return builder;
    }

    public static IFusionGatewayBuilder AddOpenApiDiagnosticEventListener<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IFusionGatewayBuilder builder) where T : class, IOpenApiDiagnosticEventListener
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureSchemaServices((_, s) => s.AddSingleton<IOpenApiDiagnosticEventListener, T>());

        return builder;
    }

    private static void AddOpenApiDocumentStorageCore(this IFusionGatewayBuilder builder)
    {
        var schemaName = builder.Name;

        builder.Services.AddSingleton<IOpenApiResultFormatter, FusionOpenApiResultFormatter>();
        builder.Services.AddOpenApiExporterServices(schemaName);

        builder.ConfigureSchemaServices((applicationServices, services)
            => services.AddOpenApiExporterSchemaServices(schemaName, applicationServices));

        builder.AddWarmupTask<OpenApiWarmupTask>();
    }
}
