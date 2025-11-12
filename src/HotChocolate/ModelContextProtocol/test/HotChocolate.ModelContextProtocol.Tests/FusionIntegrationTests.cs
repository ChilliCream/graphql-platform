using HotChocolate.Execution;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.ModelContextProtocol.Diagnostics;
using HotChocolate.ModelContextProtocol.Directives;
using HotChocolate.ModelContextProtocol.Extensions;
using HotChocolate.ModelContextProtocol.Storage;
using HotChocolate.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.Server;

namespace HotChocolate.ModelContextProtocol;

public sealed class FusionIntegrationTests : IntegrationTestBase
{
    [Fact]
    public void ListTools_AfterSchemaUpdate_ReturnsUpdatedTools()
    {
        Assert.Fail("FIXME");
    }

    protected override async Task<TestServer> CreateTestServerAsync(
        IOperationToolStorage storage,
        ITypeDefinition[]? additionalTypes = null,
        IMcpDiagnosticEventListener? diagnosticEventListener = null,
        Action<McpServerOptions>? configureMcpServerOptions = null,
        Action<IMcpServerBuilder>? configureMcpServer = null)
    {
        var subgraph = CreateSubgraph(additionalTypes);
        var schemaDocument = await subgraph.Services.GetSchemaAsync();
        var schemaComposer =
            new SchemaComposer(
                [new SourceSchemaText(schemaDocument.Name, schemaDocument.ToString())],
                new SchemaComposerOptions(),
                new CompositionLog());
        var result = schemaComposer.Compose();

        var builder = new WebHostBuilder()
            .ConfigureServices(
                services =>
                {
                    services
                        .AddHeaderPropagation(options => options.Headers.Add("Authorization"))
                        .AddLogging()
                        .AddRouting()
                        .AddAuthentication();

                    services
                        .AddHttpClient(schemaDocument.Name)
                            .AddHeaderPropagation()
                            .ConfigurePrimaryHttpMessageHandler(() => subgraph.CreateHandler());

                    var builder =
                        services
                            .AddGraphQLGatewayServer()
                            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                            .AddInMemoryConfiguration(result.Value.ToSyntaxNode())
                            .AddHttpClientConfiguration(
                                schemaDocument.Name,
                                new Uri("http://localhost:5000/graphql"))
                            .AddMcp(configureMcpServerOptions, configureMcpServer)
                            .AddMcpToolStorage(storage);

                    if (diagnosticEventListener is not null)
                    {
                        builder.AddMcpDiagnosticEventListener(diagnosticEventListener);
                    }
                })
            .Configure(
                app => app
                    .UseRouting()
                    .UseHeaderPropagation()
                    .UseAuthentication()
                    .UseEndpoints(endpoints => endpoints.MapGraphQLMcp()));

        return new TestServer(builder);
    }

    private static TestServer CreateSubgraph(ITypeDefinition[]? additionalTypes)
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(
                services =>
                {
                    services
                        .AddLogging()
                        .AddRouting()
                        .AddAuthentication()
                        .AddJwtBearer(
                            o => o.TokenValidationParameters =
                                new TokenValidationParameters
                                {
                                    ValidIssuer = TokenIssuer,
                                    ValidAudience = TokenAudience,
                                    IssuerSigningKey = TokenKey
                                });

                    var builder =
                        services
                            .AddGraphQLServer()
                            .AddAuthorization()
                            .AddQueryType<TestSchema.Query>()
                            .AddMutationType<TestSchema.Mutation>()
                            .AddInterfaceType<TestSchema.IPet>()
                            .AddUnionType<TestSchema.IPet>()
                            .AddObjectType<TestSchema.Cat>()
                            .AddObjectType<TestSchema.Dog>();

                    if (additionalTypes is not null)
                    {
                        builder.AddTypes(additionalTypes);
                    }

                    // FIXME: Where/when to add this?
                    builder.AddDirectiveType<McpToolAnnotationsDirectiveType>();
                })
            .Configure(
                app => app
                    .UseRouting()
                    .UseAuthentication()
                    .UseEndpoints(endpoints => endpoints.MapGraphQL()));

        return new TestServer(builder);
    }
}
