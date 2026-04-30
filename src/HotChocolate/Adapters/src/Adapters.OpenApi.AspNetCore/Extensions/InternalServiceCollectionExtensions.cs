using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Adapters.OpenApi;

internal static class InternalServiceCollectionExtensions
{
    public static IServiceCollection AddOpenApiAspNetCoreServices(this IServiceCollection services)
    {
        services.AddTransient<IDynamicEndpointDataSource, DynamicEndpointDataSource>();
        services.AddTransient<IDynamicOpenApiDocumentTransformer, DynamicOpenApiDocumentTransformer>();

        return services;
    }
}
