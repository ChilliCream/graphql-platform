using System.Text;
using HotChocolate.Execution;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Sdk;

namespace HotChocolate.Exporters.OpenApi;

public class FusionOpenApiIntegrationTest : OpenApiIntegrationTestBase
{
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

        _compositeSchema = result.Value.ToSyntaxNode();
    }

    protected override void ConfigureStorage(IServiceCollection services, IOpenApiDefinitionStorage storage)
    {
        services.AddGraphQLGatewayServer()
            .AddInMemoryConfiguration(_compositeSchema)
            .AddOpenApiDefinitionStorage(storage);
    }
}
