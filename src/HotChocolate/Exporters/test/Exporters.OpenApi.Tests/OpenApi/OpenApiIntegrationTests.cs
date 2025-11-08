using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Exporters.OpenApi;

public class OpenApiIntegrationTests : OpenApiIntegrationTestBase
{
    protected override void ConfigureStorage(
        IServiceCollection services,
        IOpenApiDefinitionStorage storage,
        IOpenApiDiagnosticEventListener? eventListener)
    {
        services.AddGraphQLServer()
            .AddOpenApiDefinitionStorage(storage)
            .AddBasicServer();
    }
}
