using HotChocolate.Adapters.OpenApi.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Adapters.OpenApi;

internal static class InternalServiceCollectionExtensions
{
    public static IServiceCollection AddOpenApiAspNetCoreServices(this IServiceCollection services)
    {
        services.PostConfigureAll<OpenApiTransportSetup>(static setup =>
        {
            setup.EndpointDataSourceFactory ??= static () => new DynamicEndpointDataSource();
            setup.DocumentTransformerFactory ??= static () => new DynamicOpenApiDocumentTransformer();
        });

        return services;
    }
}
