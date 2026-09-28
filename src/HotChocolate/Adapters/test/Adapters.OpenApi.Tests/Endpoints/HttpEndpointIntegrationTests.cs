using System.Net;
using System.Text;
using System.Text.Json;
using HotChocolate.Adapters.OpenApi.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Adapters.OpenApi;

public class HttpEndpointIntegrationTests : HttpEndpointIntegrationTestBase
{
    protected override void ConfigureStorage(
        IServiceCollection services,
        IOpenApiDefinitionStorage storage,
        OpenApiDiagnosticEventListener? eventListener)
    {
        var builder = services.AddGraphQLServer()
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
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddGraphQLServer("NamedSchema")
                    .AddOpenApi()
                    .AddOpenApiDefinitionStorage(storage)
                    .AddBasicServer();
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints => endpoints.MapOpenApiEndpoints());
            });
        var server = new TestServer(builder);
        var client = server.CreateClient();

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
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddGraphQLServer("NamedSchema")
                    .AddOpenApi()
                    .AddBasicServer();
                services.AddOptions<OpenApiSetup>("NamedSchema")
                    .PostConfigure(setup => setup.StorageFactory = _ => storage);
                services.AddOpenApi(options => options.AddGraphQLTransformer());
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapOpenApi();
                    endpoints.MapOpenApiEndpoints();
                });
            });
        using var server = new TestServer(builder);
        var client = server.CreateClient();

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
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddRouting();

                foreach (var schemaName in new[] { "alpha", "beta" })
                {
                    services.AddGraphQLServer(schemaName)
                        .AddOpenApi()
                        .AddBasicServer();
                    services.AddOptions<OpenApiSetup>(schemaName)
                        .PostConfigure(setup => setup.StorageFactory = _ => new TestOpenApiDefinitionStorage());
                }
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints => endpoints.MapOpenApiEndpoints());
            });

        // act
        var exception = Assert.Throws<InvalidOperationException>(() => new TestServer(builder));

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
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddGraphQLServer();
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints => endpoints.MapOpenApiEndpoints());
            });

        // act
        var exception = Assert.Throws<InvalidOperationException>(() => new TestServer(builder));

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
