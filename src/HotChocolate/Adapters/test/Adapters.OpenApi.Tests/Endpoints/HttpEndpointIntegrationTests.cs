using System.Net;
using System.Text;
using System.Text.Json;
using HotChocolate.Adapters.OpenApi.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HotChocolate.Adapters.OpenApi;

public class HttpEndpointIntegrationTests : HttpEndpointIntegrationTestBase
{
    protected override void ConfigureStorage(
        IServiceCollection services,
        IOpenApiDefinitionStorage storage,
        OpenApiDiagnosticEventListener? eventListener)
    {
        var builder = services.AddGraphQLServer()
            .ModifyCostOptions(o => o.DefaultListSize = 1)
            .AddOpenApi()
            .AddOpenApiDefinitionStorage(storage)
            .AddBasicServer();

        if (eventListener is not null)
        {
            builder.AddDiagnosticEventListener(_ => eventListener);
        }
    }

    [Fact]
    public async Task MapOpenApiEndpoints_Should_ResolveSchemaName_When_SingleNamedSchemaRegistered()
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
        var webAppBuilder = WebApplication.CreateSlimBuilder();
        webAppBuilder.WebHost.UseTestServer();
        webAppBuilder.Services.AddRouting();
        webAppBuilder.Services
            .AddGraphQLServer("NamedSchema")
            .ModifyCostOptions(o => o.DefaultListSize = 1)
            .AddOpenApi()
            .AddOpenApiDefinitionStorage(storage)
            .AddBasicServer();
        await using var webApp = webAppBuilder.Build();
        webApp.MapOpenApiEndpoints();
        webApp.Start();
        var client = webApp.GetTestClient();

        // act
        var response = await client.GetAsync("/users", TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MapOpenApiEndpointsAndAddGraphQLTransformer_Should_ResolveSchemaName_When_SingleNamedSchemaHasPostConfiguredStorage()
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
        var webAppBuilder = WebApplication.CreateSlimBuilder();
        webAppBuilder.WebHost.UseTestServer();
        webAppBuilder.Services.AddRouting();
        webAppBuilder.Services
            .AddGraphQLServer("NamedSchema")
            .ModifyCostOptions(o => o.DefaultListSize = 1)
            .AddOpenApi()
            .AddBasicServer();
        webAppBuilder.Services
            .AddOptions<OpenApiSetup>("NamedSchema")
            .PostConfigure(setup => setup.StorageFactory = _ => storage);
        webAppBuilder.Services.AddOpenApi(options => options.AddGraphQLTransformer());
        await using var webApp = webAppBuilder.Build();
        webApp.MapOpenApi();
        webApp.MapOpenApiEndpoints();
        webApp.Start();
        var client = webApp.GetTestClient();

        // act
        var response = await client.GetAsync("/users", TestContext.Current.CancellationToken);
        var document = await GetOpenApiDocumentAsync(client);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            ["/users"],
            JsonDocument.Parse(document).RootElement.GetProperty("paths").EnumerateObject().Select(p => p.Name));
    }

    [Fact]
    public void MapOpenApiEndpoints_Should_NotResolveSchemaName_When_MultipleNamedSchemasRegistered()
    {
        // arrange
        var webAppBuilder = WebApplication.CreateSlimBuilder();
        webAppBuilder.WebHost.UseTestServer();
        webAppBuilder.Services.AddRouting();

        foreach (var schemaName in new[] { "alpha", "beta" })
        {
            webAppBuilder.Services
                .AddGraphQLServer(schemaName)
                .AddOpenApi()
                .AddBasicServer();
            webAppBuilder.Services
                .AddOptions<OpenApiSetup>(schemaName)
                .PostConfigure(setup => setup.StorageFactory = _ => new TestOpenApiDefinitionStorage());
        }

        var webApp = webAppBuilder.Build();

        // act
        var exception = Assert.Throws<InvalidOperationException>(() => webApp.MapOpenApiEndpoints());

        // assert
        Assert.Equal(
            $"No IOpenApiDefinitionStorage is registered for schema '{ISchemaDefinition.DefaultName}'. "
            + "Call `AddOpenApiDefinitionStorage(...)` when configuring the GraphQL server.",
            exception.Message);
    }

    [Fact]
    public void MapOpenApiEndpoints_Should_Throw_When_AddOpenApiNotCalled()
    {
        // arrange
        var webAppBuilder = WebApplication.CreateSlimBuilder();
        webAppBuilder.WebHost.UseTestServer();
        webAppBuilder.Services.AddRouting();
        webAppBuilder.Services.AddGraphQLServer();
        var webApp = webAppBuilder.Build();

        // act
        var exception = Assert.Throws<InvalidOperationException>(() => webApp.MapOpenApiEndpoints());

        // assert
        Assert.Equal(
            "Call `AddOpenApi()` when configuring the GraphQL server.",
            exception.Message);
    }

    [Fact]
    public async Task Http_Post_Body_Field_Has_Wrong_Type()
    {
        // arrange
        var storage = CreateBasicTestDefinitionStorage();
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        var content = new StringContent(
            """
            {
              "id": "6",
              "name": "Test",
              "email": 123
            }
            """,
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/users", content, TestContext.Current.CancellationToken);

        // assert
        response.MatchSnapshot();
    }
}
