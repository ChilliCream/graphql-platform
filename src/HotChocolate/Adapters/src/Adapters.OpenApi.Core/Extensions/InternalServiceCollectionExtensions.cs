using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HotChocolate.Adapters.OpenApi;

internal static class InternalServiceCollectionExtensions
{
    public static IServiceCollection AddOpenApiServices(this IServiceCollection services, string schemaName)
    {
        services.TryAddKeyedSingleton(
            schemaName,
            static (sp, name) => new HttpRequestExecutorProxy(
                sp.GetRequiredService<IRequestExecutorProvider>(),
                sp.GetRequiredService<IRequestExecutorEvents>(),
                (string)name));

        return services;
    }

    public static IServiceCollection AddOpenApiSchemaServices(
        this IServiceCollection services,
        string schemaName,
        IServiceProvider applicationServices)
    {
        services.TryAddSingleton(schemaServices => new OpenApiDefinitionRegistry(
            schemaServices.GetRequiredService<IOpenApiDefinitionStorage>(),
            applicationServices.GetRequiredKeyedService<IDynamicOpenApiDocumentTransformer>(schemaName),
            applicationServices.GetRequiredKeyedService<IDynamicEndpointDataSource>(schemaName)
        ));

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
