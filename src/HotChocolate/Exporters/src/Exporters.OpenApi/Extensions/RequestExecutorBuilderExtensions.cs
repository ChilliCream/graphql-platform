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
    public static IRequestExecutorBuilder AddOpenApiDocumentStorage(
        this IRequestExecutorBuilder builder,
        IOpenApiDocumentStorage documentStorage)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(documentStorage);

        builder.AddOpenApiDocumentStorageCore();

        return builder.ConfigureSchemaServices(s => s.AddSingleton(documentStorage));
    }

    public static IRequestExecutorBuilder AddOpenApiDocumentStorage<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IRequestExecutorBuilder builder)
        where T : class, IOpenApiDocumentStorage
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddOpenApiDocumentStorageCore();

        return builder.ConfigureSchemaServices(static s => s.AddSingleton<IOpenApiDocumentStorage, T>());
    }

    private static void AddOpenApiDocumentStorageCore(this IRequestExecutorBuilder builder)
    {
        builder.Services.TryAddKeyedSingleton<DynamicEndpointDataSource>(builder.Name);

        builder.Services
            .AddHttpContextAccessor()
            .AddKeyedSingleton(
                builder.Name,
                // TODO: Maybe we should use a named one here to avoid conflicts in the future
                static (sp, name) => new HttpRequestExecutorProxy(
                    sp.GetRequiredService<IRequestExecutorProvider>(),
                    sp.GetRequiredService<IRequestExecutorEvents>(),
                    (string)name));

        builder.ConfigureSchemaServices(
            static (applicationServices, s) =>
                s.AddSingleton<IDynamicEndpointDataSource>(schemaServices =>
                {
                    var schemaName = schemaServices.GetRequiredService<ISchemaDefinition>().Name;
                    return applicationServices.GetRequiredKeyedService<DynamicEndpointDataSource>(schemaName);
                }));

        builder.AddWarmupTask<OpenApiWarmupTask>();
    }
}
