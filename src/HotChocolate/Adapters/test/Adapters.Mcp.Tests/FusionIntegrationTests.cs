using System.Buffers;
using System.Text.Json;
using HotChocolate.Adapters.Mcp.Configuration;
using HotChocolate.Adapters.Mcp.Diagnostics;
using HotChocolate.Adapters.Mcp.Storage;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HotChocolate.Adapters.Mcp;

public sealed class FusionIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task MapGraphQLMcp_Should_ResolveSchemaName_When_SingleNamedSchemaHasPostConfiguredStorage()
    {
        // arrange
        var storage = new TestMcpStorage();
        await storage.AddOrUpdateToolAsync(
            new OperationToolDefinition(
                Utf8GraphQLParser.Parse("query GetBooks { books { title } }")),
            TestContext.Current.CancellationToken);
        var subgraph = StartSubgraph([]);
        StopAndDisposeAfterTest(subgraph);
        var schemaDocument = await subgraph.Services.GetSchemaAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        var schemaComposer =
            new SchemaComposer(
                [new SourceSchemaText(schemaDocument.Name, schemaDocument.ToString())],
                new SchemaComposerOptions(),
                new CompositionLog());
        var result = schemaComposer.Compose();
        var webAppBuilder = WebApplication.CreateSlimBuilder();
        webAppBuilder.WebHost.UseTestServer();
        webAppBuilder.Services
            .AddRouting()
            .AddGraphQLGatewayServer("NamedSchema")
            .AddInMemoryConfiguration(result.Value.ToSyntaxNode())
            .AddMcp();
        webAppBuilder.Services
            .AddOptions<McpSetup>("NamedSchema")
            .PostConfigure(setup => setup.StorageFactory = _ => storage);
        var webApp = webAppBuilder.Build();
        webApp.MapGraphQLMcp();
        webApp.Start();
        StopAndDisposeAfterTest(webApp);
        var mcpClient = await CreateMcpClientAsync(webApp.GetTestClient());

        // act
        var tools = await mcpClient.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        Assert.Equal("get_books", Assert.Single(tools).Name);
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
                        "__resources__/GetBooksWithTitle1.graphql",
                        TestContext.Current.CancellationToken))),
            TestContext.Current.CancellationToken);
        var subgraph = StartSubgraph([]);
        StopAndDisposeAfterTest(subgraph);
        var schemaDocument = await subgraph.Services.GetSchemaAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        var schemaComposer =
            new SchemaComposer(
                [new SourceSchemaText(schemaDocument.Name, schemaDocument.ToString())],
                new SchemaComposerOptions(),
                new CompositionLog());
        var result = schemaComposer.Compose();
        var schema = result.Value;
        var initialConfig =
            new FusionConfiguration(
                schema.ToSyntaxNode(),
                new JsonDocumentOwner(JsonDocument.Parse("{ }"), new EmptyMemoryOwner()));
        var configProvider = new TestFusionConfigurationProvider(initialConfig);
        var webAppBuilder = WebApplication.CreateSlimBuilder();
        webAppBuilder.WebHost.UseTestServer();
        webAppBuilder.Services
            .AddRouting()
            .AddGraphQLGatewayServer()
            .AddConfigurationProvider(_ => configProvider)
            .AddMcp()
            .AddMcpStorage(storage);
        var webApp = webAppBuilder.Build();
        webApp.MapGraphQLMcp();
        webApp.Start();
        StopAndDisposeAfterTest(webApp);
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
        ((MutableObjectTypeDefinition)schema.Types["Book"]).Fields["title"].Description = "Description";
        var newConfig =
            new FusionConfiguration(
                schema.ToSyntaxNode(),
                new JsonDocumentOwner(JsonDocument.Parse("{ }"), new EmptyMemoryOwner()));
        configProvider.UpdateConfiguration(newConfig);
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

    protected override async Task<TestServer> CreateTestServerAsync(
        IMcpStorage storage,
        ITypeDefinition[]? additionalTypes = null,
        McpDiagnosticEventListener? diagnosticEventListener = null,
        Action<McpServerOptions>? configureMcpServerOptions = null,
        Action<IMcpServerBuilder>? configureMcpServer = null)
    {
        var subgraph = StartSubgraph(additionalTypes);
        StopAndDisposeAfterTest(subgraph);
        var schemaDocument = await subgraph.Services.GetSchemaAsync();
        var schemaComposer =
            new SchemaComposer(
                [new SourceSchemaText(schemaDocument.Name, schemaDocument.ToString())],
                new SchemaComposerOptions { Merger = { DefaultListSize = 1 } },
                new CompositionLog());
        var result = schemaComposer.Compose();

        var webAppBuilder = WebApplication.CreateSlimBuilder();
        webAppBuilder.WebHost.UseTestServer();
        webAppBuilder.Services
            .AddHeaderPropagation(options => options.Headers.Add("Authorization"))
            .AddLogging()
            .AddRouting()
            .AddAuthentication();

        webAppBuilder.Services
            .AddHttpClient(schemaDocument.Name)
                .AddHeaderPropagation()
                .ConfigurePrimaryHttpMessageHandler(subgraph.GetTestServer().CreateHandler);

        var gatewayBuilder =
            webAppBuilder.Services
                .AddGraphQLGatewayServer()
                .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                .AddInMemoryConfiguration(result.Value.ToSyntaxNode())
                .AddHttpClientConfiguration(
                    schemaDocument.Name,
                    new Uri("http://localhost:5000/graphql"))
                .AddMcp(configureMcpServerOptions, configureMcpServer)
                .AddMcpStorage(storage);

        if (diagnosticEventListener is not null)
        {
            gatewayBuilder.AddDiagnosticEventListener(_ => diagnosticEventListener);
        }

        var app = webAppBuilder.Build();
        app.UseRouting();
        app.UseHeaderPropagation();
        app.UseAuthentication();
        app.MapGraphQLMcp();
        app.Start();
        StopAndDisposeAfterTest(app);

        return app.GetTestServer();
    }

    private static WebApplication StartSubgraph(ITypeDefinition[]? additionalTypes)
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
                .AddSourceSchemaDefaults()
                .ModifyCostOptions(o => o.DefaultListSize = 1)
                .AddAuthorization()
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

        var app = webAppBuilder.Build();
        app.UseRouting();
        app.UseAuthentication();
        app.MapGraphQL();
        app.Start();

        return app;
    }

    private sealed class TestFusionConfigurationProvider(FusionConfiguration initialConfig)
        : IFusionConfigurationProvider
    {
        private readonly List<IObserver<FusionConfiguration>> _observers = [];

        public IDisposable Subscribe(IObserver<FusionConfiguration> observer)
        {
            if (Configuration is not null)
            {
                observer.OnNext(Configuration);
            }

            _observers.Add(observer);

            return new Observer();
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public FusionConfiguration? Configuration { get; private set; } = initialConfig;

        public void UpdateConfiguration(FusionConfiguration configuration)
        {
            Configuration = configuration;

            foreach (var observer in _observers)
            {
                observer.OnNext(Configuration);
            }
        }

        private sealed class Observer : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }

    private sealed class EmptyMemoryOwner : IMemoryOwner<byte>
    {
        public Memory<byte> Memory => default;

        public void Dispose() { }
    }
}
