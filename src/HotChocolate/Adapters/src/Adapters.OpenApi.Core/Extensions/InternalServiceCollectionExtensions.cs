using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HotChocolate.Adapters.OpenApi;

internal static class InternalServiceCollectionExtensions
{
    public static IServiceCollection AddOpenApiServices(
        this IServiceCollection applicationServices,
        string schemaName)
    {
        applicationServices.TryAddKeyedSingleton(
            schemaName,
            static (sp, name) => new OpenApiDefinitionRegistry(
                sp.GetRequiredKeyedService<IOpenApiDefinitionStorage>(name),
                sp.GetRequiredKeyedService<IDynamicOpenApiDocumentTransformer>(name),
                sp.GetRequiredKeyedService<IDynamicEndpointDataSource>(name)));

        applicationServices.TryAddKeyedSingleton(
            schemaName,
            static (sp, name) => new HttpRequestExecutorProxy(
                sp.GetRequiredService<IRequestExecutorProvider>(),
                sp.GetRequiredService<IRequestExecutorEvents>(),
                (string)name));

        return applicationServices;
    }

    public static IServiceCollection AddOpenApiSchemaServices(
        this IServiceCollection schemaServices)
    {
        schemaServices.TryAddSingleton<IOpenApiDiagnosticEvents>(sp =>
        {
            var listeners = sp.GetServices<IOpenApiDiagnosticEventListener>().ToArray();
            return listeners.Length switch
            {
                0 => new NoOpOpenApiDiagnosticEventListener(),
                1 => listeners[0],
                _ => new AggregateOpenApiDiagnosticEventListener(listeners)
            };
        });

        return schemaServices;
    }
}
