using System.Text.Json;
using HotChocolate.Adapters.Mcp.Configuration;
using HotChocolate.Adapters.Mcp.Diagnostics;
using HotChocolate.Adapters.Mcp.Storage;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HotChocolate.Adapters.Mcp;

public sealed class CoreIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task MapGraphQLMcp_Should_ResolveSchemaName_When_SingleNamedSchemaRegistered()
    {
        // arrange
        var storage = new TestMcpStorage();
        await storage.AddOrUpdateToolAsync(
            new OperationToolDefinition(
                Utf8GraphQLParser.Parse("query GetBooks { books { title } }")),
            TestContext.Current.CancellationToken);
        var webAppBuilder = WebApplication.CreateSlimBuilder();
        webAppBuilder.WebHost.UseTestServer();
        webAppBuilder.Services
            .AddRouting()
            .AddGraphQL("NamedSchema")
            .AddAuthorization()
            .AddQueryType<TestSchema.Query>()
            .AddMutationType<TestSchema.Mutation>()
            .AddInterfaceType<TestSchema.IPet>()
            .AddUnionType<TestSchema.IPet>()
            .AddObjectType<TestSchema.Cat>()
            .AddObjectType<TestSchema.Dog>()
            .AddMcp()
            .AddMcpStorage(storage);
        await using var webApp = webAppBuilder.Build();
        webApp.MapGraphQLMcp();
        webApp.Start();
        var mcpClient = await CreateMcpClientAsync(webApp.GetTestClient());

        // act
        var tools = await mcpClient.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        Assert.Equal("get_books", Assert.Single(tools).Name);
    }

    [Fact]
    public async Task MapGraphQLMcp_Should_ResolveSchemaName_When_SingleNamedSchemaHasPostConfiguredStorage()
    {
        // arrange
        var storage = new TestMcpStorage();
        await storage.AddOrUpdateToolAsync(
            new OperationToolDefinition(
                Utf8GraphQLParser.Parse("query GetBooks { books { title } }")),
            TestContext.Current.CancellationToken);
        var webAppBuilder = WebApplication.CreateSlimBuilder();
        webAppBuilder.WebHost.UseTestServer();
        webAppBuilder.Services
            .AddRouting()
            .AddGraphQL("NamedSchema")
            .AddAuthorization()
            .AddQueryType<TestSchema.Query>()
            .AddMutationType<TestSchema.Mutation>()
            .AddInterfaceType<TestSchema.IPet>()
            .AddUnionType<TestSchema.IPet>()
            .AddObjectType<TestSchema.Cat>()
            .AddObjectType<TestSchema.Dog>()
            .AddMcp();
        webAppBuilder.Services
            .AddOptions<McpSetup>("NamedSchema")
            .PostConfigure(setup => setup.StorageFactory = _ => storage);
        await using var webApp = webAppBuilder.Build();
        webApp.MapGraphQLMcp();
        webApp.Start();
        var mcpClient = await CreateMcpClientAsync(webApp.GetTestClient());

        // act
        var tools = await mcpClient.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        Assert.Equal("get_books", Assert.Single(tools).Name);
    }

    [Fact]
    public void MapGraphQLMcp_Should_Throw_When_AddMcpNotCalled()
    {
        // arrange
        var webAppBuilder = WebApplication.CreateSlimBuilder();
        webAppBuilder.WebHost.UseTestServer();
        webAppBuilder.Services
            .AddRouting()
            .AddGraphQL()
            .AddQueryType<TestSchema.Query>();
        var webApp = webAppBuilder.Build();

        // act
        var exception = Assert.Throws<InvalidOperationException>(() => webApp.MapGraphQLMcp());

        // assert
        Assert.Equal(
            "Call `AddMcp()` when configuring the GraphQL server.",
            exception.Message);
    }

    [Fact]
    public void MapGraphQLMcp_Should_NotBuildSchema_When_Mapping()
    {
        // arrange
        var schemaBuilt = false;
        var webAppBuilder = WebApplication.CreateSlimBuilder();
        webAppBuilder.WebHost.UseTestServer();
        webAppBuilder.Services
            .AddRouting()
            .AddGraphQLServer()
            .ConfigureSchemaServices(_ => schemaBuilt = true)
            .AddAuthorization()
            .AddQueryType<TestSchema.Query>()
            .AddMutationType<TestSchema.Mutation>()
            .AddInterfaceType<TestSchema.IPet>()
            .AddUnionType<TestSchema.IPet>()
            .AddObjectType<TestSchema.Cat>()
            .AddObjectType<TestSchema.Dog>()
            .AddMcp()
            .AddMcpStorage(new TestMcpStorage());
        var webApp = webAppBuilder.Build();

        // act
        webApp.MapGraphQLMcp();

        // assert
        Assert.False(schemaBuilt);
    }

    [Fact]
    public async Task ListTools_AfterSchemaUpdate_ReturnsUpdatedTools()
    {
        // arrange
        var storage = new TestMcpStorage();
        await storage.AddOrUpdateToolAsync(
            new OperationToolDefinition(
                Utf8GraphQLParser.Parse(
                    await File.ReadAllTextAsync(
                        "__resources__/GetSingleField.graphql",
                        TestContext.Current.CancellationToken))),
            TestContext.Current.CancellationToken);
        var typeModule = new TestTypeModule();
        var webAppBuilder = WebApplication.CreateSlimBuilder();
        webAppBuilder.WebHost.UseTestServer();
        webAppBuilder.Services
            .AddRouting()
            .AddGraphQL()
            .AddTypeModule(_ => typeModule)
            .AddMcp()
            .AddMcpStorage(storage);
        await using var webApp = webAppBuilder.Build();
        webApp.MapGraphQLMcp();
        webApp.Start();
        var mcpClient1 = await CreateMcpClientAsync(webApp.GetTestClient());
        var mcpClient2 = await CreateMcpClientAsync(webApp.GetTestClient());
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
        var tools = await mcpClient1.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
        typeModule.TriggerChange();
        IList<McpClientTool>? updatedTools = null;

        if (listChangedResetEvent1.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken))
        {
            var mcpClient3 = await CreateMcpClientAsync(webApp.GetTestClient());
            updatedTools = await mcpClient3.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
        }

        var secondClientNotified = listChangedResetEvent2.Wait(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken);

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
        var webAppBuilder = WebApplication.CreateSlimBuilder();
        webAppBuilder.WebHost.UseTestServer();
        webAppBuilder.Services
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

        var graphqlBuilder =
            webAppBuilder.Services
                .AddGraphQLServer()
                .ModifyCostOptions(o => o.DefaultListSize = 1)
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
            graphqlBuilder.AddTypes(additionalTypes);
        }

        if (diagnosticEventListener is not null)
        {
            graphqlBuilder.AddDiagnosticEventListener(_ => diagnosticEventListener);
        }

        var app = webAppBuilder.Build();
        app.UseRouting();
        app.UseAuthentication();
        app.MapGraphQLMcp();
        app.Start();

        return Task.FromResult(app.GetTestServer());
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
