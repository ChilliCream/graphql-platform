using System.Net;
using System.Text;
using System.Text.Json;
using HotChocolate.Adapters.OpenApi.Configuration;
using HotChocolate.Execution;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Language;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Sdk;

namespace HotChocolate.Adapters.OpenApi;

public class FusionHttpEndpointIntegrationTests : HttpEndpointIntegrationTestBase
{
    private TestServer _subgraph = null!;
    private DocumentNode _compositeSchema = null!;

    protected override async ValueTask InitializeAsync(TestServerSession serverSession)
    {
        var server = CreateSourceSchema();

        var schema = await server.Services.GetSchemaAsync();
        var sourceSchemaText = new SourceSchemaText("A", schema.ToString());

        var compositionLog = new CompositionLog();
        var composerOptions = new SchemaComposerOptions
        {
            Merger =
            {
                EnableGlobalObjectIdentification = true
            }
        };
        var composer = new SchemaComposer([sourceSchemaText], composerOptions, compositionLog);
        var result = composer.Compose();

        if (!result.IsSuccess)
        {
            var sb = new StringBuilder();
            sb.Append(result.Errors[0].Message);

            foreach (var entry in compositionLog)
            {
                sb.AppendLine();
                sb.Append(entry.Message);
            }

            throw new XunitException(sb.ToString());
        }

        _subgraph = server;
        _compositeSchema = result.Value.ToSyntaxNode();
    }

    protected override void ConfigureStorage(
        IServiceCollection services,
        IOpenApiDefinitionStorage storage,
        OpenApiDiagnosticEventListener? eventListener)
    {
        services.AddHttpClient("A")
            .ConfigurePrimaryHttpMessageHandler(_subgraph.CreateHandler)
            .AddHeaderPropagation();

        var builder = services.AddGraphQLGatewayServer()
            .AddInMemoryConfiguration(_compositeSchema)
            .AddHttpClientConfiguration("A", new Uri("http://localhost:5000/graphql"))
            .AddOpenApi()
            .AddOpenApiDefinitionStorage(storage);

        if (eventListener is not null)
        {
            builder.AddDiagnosticEventListener(_ => eventListener);
        }
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
                services.AddHttpClient("A")
                    .ConfigurePrimaryHttpMessageHandler(_subgraph.CreateHandler);
                services.AddGraphQLGatewayServer("NamedSchema")
                    .AddInMemoryConfiguration(_compositeSchema)
                    .AddHttpClientConfiguration("A", new Uri("http://localhost:5000/graphql"))
                    .AddOpenApi();
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
                    services.AddGraphQLGatewayServer(schemaName)
                        .AddInMemoryConfiguration(_compositeSchema)
                        .AddOpenApi();
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
