using System.Diagnostics.CodeAnalysis;
using HotChocolate;
using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Exporters.OpenApi;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

// TODO: Also add Fusion variants
public static class RequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddOpenApiDefinitionStorage(
        this IRequestExecutorBuilder builder,
        IOpenApiDefinitionStorage definitionStorage)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(definitionStorage);

        builder.AddOpenApiDocumentStorageCore();

        return builder.ConfigureSchemaServices(s => s.AddSingleton(definitionStorage));
    }

    public static IRequestExecutorBuilder AddOpenApiDefinitionStorage<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IRequestExecutorBuilder builder)
        where T : class, IOpenApiDefinitionStorage
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddOpenApiDocumentStorageCore();

        return builder.ConfigureSchemaServices(static s => s.AddSingleton<IOpenApiDefinitionStorage, T>());
    }

    private static void AddOpenApiDocumentStorageCore(this IRequestExecutorBuilder builder)
    {
        // EndpointDataSource
        builder.Services.TryAddKeyedSingleton<DynamicEndpointDataSource>(builder.Name);

        builder.ConfigureSchemaServices(
            static (applicationServices, s) =>
                s.AddSingleton<IDynamicEndpointDataSource>(schemaServices =>
                {
                    var schemaName = schemaServices.GetRequiredService<ISchemaDefinition>().Name;
                    return applicationServices.GetRequiredKeyedService<DynamicEndpointDataSource>(schemaName);
                }));

        // DynamicOpenApiDocumentTransformer
        builder.Services.TryAddKeyedSingleton<DynamicOpenApiDocumentTransformer>(builder.Name);

        builder.ConfigureSchemaServices(
            static (applicationServices, s) =>
                s.AddSingleton(schemaServices =>
                {
                    var schemaName = schemaServices.GetRequiredService<ISchemaDefinition>().Name;
                    return applicationServices.GetRequiredKeyedService<DynamicOpenApiDocumentTransformer>(schemaName);
                }));

        // RequestExecutorProxy
        builder.Services
            .AddHttpContextAccessor()
            .AddKeyedSingleton(
                builder.Name,
                // TODO: Maybe we should create our own implementation here?
                static (sp, name) => new HttpRequestExecutorProxy(
                    sp.GetRequiredService<IRequestExecutorProvider>(),
                    sp.GetRequiredService<IRequestExecutorEvents>(),
                    (string)name));

        builder.AddWarmupTask<OpenApiWarmupTask>();
    }
}
