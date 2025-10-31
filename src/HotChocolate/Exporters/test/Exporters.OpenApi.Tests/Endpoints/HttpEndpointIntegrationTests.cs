using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Exporters.OpenApi;

public class HttpEndpointIntegrationTests : HttpEndpointIntegrationTestBase
{
    protected override void ConfigureStorage(IServiceCollection services, IOpenApiDefinitionStorage storage)
    {
        services.AddGraphQLServer()
            .AddOpenApiDefinitionStorage(storage)
            .AddBasicServer();
    }
}
