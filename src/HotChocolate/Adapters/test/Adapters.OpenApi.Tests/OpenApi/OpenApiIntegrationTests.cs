using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
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

    [Fact]
    public async Task AddGraphQLTransformer_Should_ResolveSchemaName_When_SingleNamedSchemaRegistered()
    {
        // arrange
        var storage = new TestOpenApiDefinitionStorage(
            """
            query GetUsers @http(method: GET, route: "/users") {
              usersWithoutAuth {
                id
              }
            }
            """);
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddGraphQLServer("NamedSchema")
                    .AddOpenApiDefinitionStorage(storage)
                    .AddBasicServer();
                services.AddOpenApi(options => options.AddGraphQLTransformer());
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints => endpoints.MapOpenApi());
            });
        using var server = new TestServer(builder);
        var client = server.CreateClient();

        // act
        var document = await GetOpenApiDocumentAsync(client);

        // assert
        Assert.Contains("/users", document);
    }
}
