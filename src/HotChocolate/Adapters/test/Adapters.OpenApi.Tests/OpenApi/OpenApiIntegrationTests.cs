using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Adapters.OpenApi;

public class OpenApiIntegrationTests : OpenApiIntegrationTestBase
{
    protected override void ConfigureStorage(
        IServiceCollection services,
        IOpenApiDefinitionStorage storage,
        OpenApiDiagnosticEventListener? eventListener)
    {
        services.AddGraphQLServer()
            .AddOpenApiDefinitionStorage(storage)
            .AddBasicServer();
    }
}
