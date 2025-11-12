using System.Net;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Adapters.OpenApi;

public class HttpEndpointIntegrationTests : HttpEndpointIntegrationTestBase
{
    protected override void ConfigureStorage(
        IServiceCollection services,
        IOpenApiDefinitionStorage storage,
        IOpenApiDiagnosticEventListener? eventListener)
    {
        var builder = services.AddGraphQLServer()
            .AddOpenApiDefinitionStorage(storage)
            .AddBasicServer();

        if (eventListener is not null)
        {
            builder.AddOpenApiDiagnosticEventListener(eventListener);
        }
    }
}
