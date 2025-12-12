using System.Text.Json;
using HotChocolate.Adapters.Mcp.Diagnostics;
using HotChocolate.Adapters.Mcp.Extensions;
using HotChocolate.Adapters.Mcp.Storage;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HotChocolate.Adapters.Mcp;

public sealed class CoreIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task ListTools_AfterSchemaUpdate_ReturnsUpdatedTools()
    {
        // arrange
        var storage = new TestMcpStorage();
        await storage.AddOrUpdateToolAsync(
            new OperationToolDefinition(
                Utf8GraphQLParser.Parse(
                    await File.ReadAllTextAsync("__resources__/GetSingleField.graphql"))));
        var typeModule = new TestTypeModule();
        var builder = new WebHostBuilder()
            .ConfigureServices(
                services => services
                    .AddRouting()
                    .AddGraphQL()
                    .AddTypeModule(_ => typeModule)
                    .AddMcp()
                    .AddMcpStorage(storage))
            .Configure(
                app => app
                    .UseRouting()
                    .UseEndpoints(endpoints => endpoints.MapGraphQLMcp()));
        var server = new TestServer(builder);
        var mcpClient1 = await CreateMcpClientAsync(server.CreateClient());
        var mcpClient2 = await CreateMcpClientAsync(server.CreateClient());
        var listChangedResetEvent1 = new ManualResetEventSlim(false);
        var listChangedResetEvent2 = new ManualResetEventSlim(false);
        mcpClient1.RegisterNotificationHandler(
            NotificationMethods.ToolListChangedNotification,
            async (_, _) =>
            {
                listChangedResetEvent1.Set();
                await ValueTask.CompletedTask;
            });
        mcpClient2.RegisterNotificationHandler(
            NotificationMethods.ToolListChangedNotification,
            async (_, _) =>
            {
                listChangedResetEvent2.Set();
                await ValueTask.CompletedTask;
            });

        // act
        var tools = await mcpClient1.ListToolsAsync();
        typeModule.TriggerChange();
        IList<McpClientTool>? updatedTools = null;

        if (listChangedResetEvent1.Wait(TimeSpan.FromSeconds(5)))
        {
            var mcpClient3 = await CreateMcpClientAsync(server.CreateClient());
            updatedTools = await mcpClient3.ListToolsAsync();
        }

        var secondClientNotified = listChangedResetEvent2.Wait(TimeSpan.FromSeconds(5));

        // assert
        Assert.NotNull(updatedTools);
        JsonSerializer.Serialize(
                tools.Concat(updatedTools).Select(
                    t =>
                        new
                        {
                            t.Name,
                            t.Title,
                            t.Description,
                            t.JsonSchema,
                            t.ReturnJsonSchema
                        }),
                JsonSerializerOptions)
            .ReplaceLineEndings("\n")
            .MatchSnapshot(extension: ".json");
        Assert.True(secondClientNotified);
    }

    protected override Task<TestServer> CreateTestServerAsync(
        IMcpStorage storage,
        ITypeDefinition[]? additionalTypes = null,
        McpDiagnosticEventListener? diagnosticEventListener = null,
        Action<McpServerOptions>? configureMcpServerOptions = null,
        Action<IMcpServerBuilder>? configureMcpServer = null)
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
                            .AddGraphQL()
                            .AddAuthorization()
                            .AddMcp(configureMcpServerOptions, configureMcpServer)
                            .AddMcpStorage(storage)
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

                    if (diagnosticEventListener is not null)
                    {
                        builder.AddDiagnosticEventListener(_ => diagnosticEventListener);
                    }
                })
            .Configure(
                app => app
                    .UseRouting()
                    .UseAuthentication()
                    .UseEndpoints(endpoints => endpoints.MapGraphQLMcp()));

        return Task.FromResult(new TestServer(builder));
    }

    private sealed class TestTypeModule : TypeModule
    {
        private int _executionCount;

        public override ValueTask<IReadOnlyCollection<ITypeSystemMember>> CreateTypesAsync(
            IDescriptorContext context,
            CancellationToken cancellationToken)
        {
            var types = new List<ITypeSystemMember>();

            var queryType = new ObjectTypeConfiguration(OperationTypeNames.Query);

            queryType.Fields.Add(
                new ObjectFieldConfiguration(
                    "field",
                    $"Field description {_executionCount}.",
                    type: TypeReference.Parse(_executionCount == 0 ? "Int!" : "String"),
                    pureResolver: _ => _executionCount));
            types.Add(ObjectType.CreateUnsafe(queryType));

            _executionCount++;

            return new ValueTask<IReadOnlyCollection<ITypeSystemMember>>(types);
        }

        public void TriggerChange() => OnTypesChanged();
    }
}
