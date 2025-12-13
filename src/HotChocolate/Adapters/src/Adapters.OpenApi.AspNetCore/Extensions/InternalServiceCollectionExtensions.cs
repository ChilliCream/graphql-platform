using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HotChocolate.Adapters.OpenApi;

internal static class InternalServiceCollectionExtensions
{
    public static IServiceCollection AddOpenApiAspNetCoreServices(this IServiceCollection services, string schemaName)
    {
        services.TryAddKeyedSingleton<DynamicDynamicOpenApiDocumentTransformer>(schemaName);
        services.TryAddKeyedSingleton<IDynamicOpenApiDocumentTransformer>(
            schemaName,
            (sp, key) => sp.GetRequiredKeyedService<DynamicDynamicOpenApiDocumentTransformer>(key));

        return services;
    }
}
