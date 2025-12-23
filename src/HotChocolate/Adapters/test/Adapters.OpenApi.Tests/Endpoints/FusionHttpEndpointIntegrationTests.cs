using System.Text;
using HotChocolate.Execution;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Language;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Sdk;

namespace HotChocolate.Adapters.OpenApi;

public class FusionHttpEndpointIntegrationTests : HttpEndpointIntegrationTestBase
{
    private TestServer _subgraph = null!;
    private DocumentNode _compositeSchema = null!;

    protected override async Task InitializeAsync(TestServerSession serverSession)
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
            .AddOpenApiDefinitionStorage(storage);

        if (eventListener is not null)
        {
            builder.AddDiagnosticEventListener(_ => eventListener);
        }
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

        var response = await client.PostAsync("/users", content);

        // assert
        response.MatchSnapshot();
    }
}
