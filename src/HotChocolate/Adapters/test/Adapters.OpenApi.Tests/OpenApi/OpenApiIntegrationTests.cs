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
            .AddOpenApi()
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
                    .AddOpenApi()
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

    [Fact]
    public async Task SelfRef_Input_Type_Via_Mutation_Body()
    {
        // arrange
        var storage = new TestOpenApiDefinitionStorage(
            """
            mutation SubmitSelfRef($input: SelfReferencingInput! @body)
              @http(method: POST, route: "/self-ref") {
              submitSelfRef(input: $input)
            }
            """);
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        var document = await GetOpenApiDocumentAsync(client);

        // assert
        document.MatchSnapshot(postFix: TestEnvironment.TargetFramework, extension: ".json");
    }

    [Fact]
    public async Task SelfRef_Output_Type_Via_Query()
    {
        // arrange
        var storage = new TestOpenApiDefinitionStorage(
            """
            query GetTree @http(method: GET, route: "/tree") {
              tree {
                value
                children {
                  value
                }
              }
            }
            """);
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        var document = await GetOpenApiDocumentAsync(client);

        // assert
        document.MatchSnapshot(postFix: TestEnvironment.TargetFramework, extension: ".json");
    }

    [Fact]
    public async Task Indirect_Input_Type_Via_Mutation_Body()
    {
        // arrange
        // A -> B -> A: the body inlines IndirectParentInput, then the cycle closes through
        // the IndirectMiddleInput and IndirectParentInput components.
        var storage = new TestOpenApiDefinitionStorage(
            """
            mutation SubmitIndirect($input: IndirectParentInput! @body)
              @http(method: POST, route: "/indirect-input") {
              submitIndirect(input: $input)
            }
            """);
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        var document = await GetOpenApiDocumentAsync(client);

        // assert
        document.MatchSnapshot(postFix: TestEnvironment.TargetFramework, extension: ".json");
    }

    [Fact]
    public async Task Indirect_Output_Type_Via_Query()
    {
        // arrange
        // A -> B -> A selected a couple of levels deep so the expansion is bounded by the
        // selection set rather than the type cycle.
        var storage = new TestOpenApiDefinitionStorage(
            """
            query GetIndirect @http(method: GET, route: "/indirect-output") {
              indirect {
                value
                middle {
                  label
                  parent {
                    value
                    middle {
                      label
                    }
                  }
                }
              }
            }
            """);
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        var document = await GetOpenApiDocumentAsync(client);

        // assert
        document.MatchSnapshot(postFix: TestEnvironment.TargetFramework, extension: ".json");
    }

    [Fact]
    public async Task Indirect_Input_Type_With_Two_Sibling_Occurrences()
    {
        // arrange
        // The same input type appears at two sibling positions and both must emit a $ref.
        var storage = new TestOpenApiDefinitionStorage(
            """
            mutation SubmitTwoChildren($input: TwoChildrenInput! @body)
              @http(method: POST, route: "/two-children") {
              submitTwoChildren(input: $input)
            }
            """);
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        var document = await GetOpenApiDocumentAsync(client);

        // assert
        document.MatchSnapshot(postFix: TestEnvironment.TargetFramework, extension: ".json");
    }
}
