using HotChocolate.Adapters.OpenApi.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Adapters.OpenApi;

internal static class InternalServiceCollectionExtensions
{
    public static IServiceCollection AddOpenApiAspNetCoreServices(this IServiceCollection services)
    {
        services.PostConfigureAll<OpenApiSetup>(static setup =>
        {
            setup.EndpointDataSourceFactory ??= static (_, _) => new DynamicEndpointDataSource();
            setup.DocumentTransformerFactory ??= static (_, _) => new DynamicOpenApiDocumentTransformer();
        });

        return services;
    }
}
