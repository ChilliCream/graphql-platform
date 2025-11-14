using System.Buffers;
using System.Text.Json;
using CookieCrumble;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Language;
using HotChocolate.ModelContextProtocol.Diagnostics;
using HotChocolate.ModelContextProtocol.Extensions;
using HotChocolate.ModelContextProtocol.Storage;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HotChocolate.ModelContextProtocol;

public sealed class FusionIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task ListTools_AfterSchemaUpdate_ReturnsUpdatedTools()
    {
        // arrange
        var storage = new TestOperationToolStorage();
        await storage.AddOrUpdateToolAsync(
            Utf8GraphQLParser.Parse(
                await File.ReadAllTextAsync("__resources__/GetBooksWithTitle1.graphql")));
        var subgraph = CreateSubgraph([]);
        var schemaDocument = await subgraph.Services.GetSchemaAsync();
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
        var builder = new WebHostBuilder()
            .ConfigureServices(
                services => services
                    .AddRouting()
                    .AddGraphQLGatewayServer()
                    .AddConfigurationProvider(_ => configProvider)
                    .AddMcp()
                    .AddMcpToolStorage(storage))
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
        ((MutableObjectTypeDefinition)schema.Types["Book"]).Fields["title"].Description = "Description";
        var newConfig =
            new FusionConfiguration(
                schema.ToSyntaxNode(),
                new JsonDocumentOwner(JsonDocument.Parse("{ }"), new EmptyMemoryOwner()));
        configProvider.UpdateConfiguration(newConfig);
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

    protected override async Task<TestServer> CreateTestServerAsync(
        IOperationToolStorage storage,
        ITypeDefinition[]? additionalTypes = null,
        McpDiagnosticEventListener? diagnosticEventListener = null,
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
                        builder.AddDiagnosticEventListener(_ => diagnosticEventListener);
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
                })
            .Configure(
                app => app
                    .UseRouting()
                    .UseAuthentication()
                    .UseEndpoints(endpoints => endpoints.MapGraphQL()));

        return new TestServer(builder);
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
