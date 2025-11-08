using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HotChocolate.Exporters.OpenApi;

internal static class InternalServiceCollectionExtensions
{
    public static IServiceCollection AddOpenApiExporterServices(this IServiceCollection services, string schemaName)
    {
        services.TryAddKeyedSingleton<DynamicEndpointDataSource>(schemaName);
        services.TryAddKeyedSingleton<DynamicOpenApiDocumentTransformer>(schemaName);
        services.TryAddKeyedSingleton(
            schemaName,
            static (sp, name) => new OpenApiDocumentManager(
                sp.GetRequiredKeyedService<IOpenApiDefinitionStorage>(name),
                sp.GetRequiredKeyedService<DynamicOpenApiDocumentTransformer>(name),
                sp.GetRequiredKeyedService<DynamicEndpointDataSource>(name)
                ));
        services.TryAddKeyedSingleton(
            schemaName,
            static (sp, name) => new HttpRequestExecutorProxy(
                sp.GetRequiredService<IRequestExecutorProvider>(),
                sp.GetRequiredService<IRequestExecutorEvents>(),
                (string)name));

        return services;
    }

    public static IServiceCollection AddOpenApiExporterSchemaServices(
        this IServiceCollection services,
        string schemaName,
        IServiceProvider applicationServices)
    {
        services.TryAddSingleton(
            _ => applicationServices.GetRequiredKeyedService<IOpenApiDefinitionStorage>(schemaName));

        services.TryAddSingleton(
            _ => applicationServices.GetRequiredKeyedService<OpenApiDocumentManager>(schemaName));

        services.TryAddSingleton<IDynamicEndpointDataSource>(
            _ => applicationServices.GetRequiredKeyedService<DynamicEndpointDataSource>(schemaName));

        services.TryAddSingleton(
            _ => applicationServices.GetRequiredKeyedService<DynamicOpenApiDocumentTransformer>(schemaName));

        services.TryAddSingleton<IOpenApiDiagnosticEvents>(sp =>
        {
            var listeners = sp.GetServices<IOpenApiDiagnosticEventListener>().ToArray();
            return listeners.Length switch
            {
                0 => new NoOpOpenApiDiagnosticEventListener(),
                1 => listeners[0],
                _ => new AggregateOpenApiDiagnosticEventListener(listeners)
            };
        });

        return services;
    }
}
