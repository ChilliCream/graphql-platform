using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Adapters.OpenApi;

public class ValidationTests : ValidationTestBase
{
    protected override void ConfigureStorage(
        IServiceCollection services,
        IOpenApiDocumentStorage storage,
        OpenApiDiagnosticEventListener? eventListener)
    {
        var builder = services.AddGraphQLServer()
            .AddOpenApiDocumentStorage(storage)
            .AddBasicServer();

        if (eventListener is not null)
        {
            builder.AddDiagnosticEventListener(_ => eventListener);
        }
    }
}
