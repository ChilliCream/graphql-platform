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
        services.AddSingleton<IDynamicEndpointDataSource>(
            _ => applicationServices.GetRequiredKeyedService<DynamicEndpointDataSource>(schemaName));

        services.AddSingleton(
            _ => applicationServices.GetRequiredKeyedService<DynamicOpenApiDocumentTransformer>(schemaName));

        return services;
    }
}
