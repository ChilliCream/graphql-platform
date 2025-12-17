using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HotChocolate.Adapters.OpenApi;

internal static class InternalServiceCollectionExtensions
{
    public static IServiceCollection AddOpenApiAspNetCoreServices(this IServiceCollection services, string schemaName)
    {
        services.TryAddKeyedSingleton<DynamicOpenApiDocumentTransformer>(schemaName);
        services.TryAddKeyedSingleton<IDynamicOpenApiDocumentTransformer>(
            schemaName,
            (sp, key) => sp.GetRequiredKeyedService<DynamicOpenApiDocumentTransformer>(key));

        return services;
    }
}
