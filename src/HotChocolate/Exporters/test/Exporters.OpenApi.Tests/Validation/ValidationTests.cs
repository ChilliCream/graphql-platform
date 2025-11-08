using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Exporters.OpenApi;

public class ValidationTests : ValidationTestBase
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
