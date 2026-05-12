#if NET9_0_OR_GREATER
using System.Text.RegularExpressions;
using HotChocolate.Adapters.Mcp.Storage;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using static HotChocolate.Fusion.Diagnostics.ActivityTestHelper;

namespace HotChocolate.Fusion.Diagnostics;

[Collection("Instrumentation")]
public partial class FusionMcpAdapterActivityTests : FusionTestBase
{
    [Fact]
    public async Task Mcp_CallTool_Field_Does_Not_Exist()
    {
        // arrange
        using var server = await CreateGatewayAsync("query InvalidGraphqlQuery { doesNotExist }");
        var mcpClient = await CreateMcpClientAsync(server.CreateClient());
        await mcpClient.ListToolsAsync();

        using (CaptureActivities(out var activities))
        {
            // act
            await mcpClient.CallToolAsync("invalid_graphql_query");

            // assert
            MatchActivitySnapshot(activities);
        }
    }

    [Fact]
    public async Task Mcp_CallTool_Tool_Does_Not_Exist()
    {
        // arrange
        using var server = await CreateGatewayAsync("query GetBook { book { title } }");
        var mcpClient = await CreateMcpClientAsync(server.CreateClient());
        await mcpClient.ListToolsAsync();

        using (CaptureActivities(out var activities))
        {
            // act
            await mcpClient.CallToolAsync("does_not_exist");

            // assert
            MatchActivitySnapshot(activities);
        }
    }

    [Fact]
    public async Task Mcp_CallTool()
    {
        // arrange
        using var server = await CreateGatewayAsync("query GetBook { book { title } }");
        var mcpClient = await CreateMcpClientAsync(server.CreateClient());
        await mcpClient.ListToolsAsync();

        using (CaptureActivities(out var activities))
        {
            // act
            await mcpClient.CallToolAsync("get_book");

            // assert
            MatchActivitySnapshot(activities);
        }
    }

    [Fact]
    public async Task Mcp_CallTool_GraphQL_Field_Error()
    {
        // arrange
        using var server = await CreateGatewayAsync("query GetFaultyBook { faultyBook { title } }");
        var mcpClient = await CreateMcpClientAsync(server.CreateClient());
        await mcpClient.ListToolsAsync();

        using (CaptureActivities(out var activities))
        {
            // act
            await mcpClient.CallToolAsync("get_faulty_book");

            // assert
            MatchActivitySnapshot(activities);
        }
    }

    [Fact]
    public async Task Mcp_GetPrompt()
    {
        // arrange
        using var server = await CreateGatewayAsync(prompt: CreateGreetingPrompt());
        var mcpClient = await CreateMcpClientAsync(server.CreateClient());
        await mcpClient.ListPromptsAsync();

        using (CaptureActivities(out var activities))
        {
            // act
            await mcpClient.GetPromptAsync("greeting");

            // assert
            MatchActivitySnapshot(activities);
        }
    }

    [GeneratedRegex("""("Key": "mcp\.session\.id",\s*"Value": ")[^"]*(")""")]
    private static partial Regex SessionIdRegex();

    private static void MatchActivitySnapshot(object activities)
    {
        var serializer = JsonSerializer.Create(
            new JsonSerializerSettings { Converters = { new StringEnumConverter() } });
        var capture = JObject.FromObject(activities, serializer);

        // The MCP client keeps a streamable-HTTP session open and processes
        // notifications fire-and-forget, so the top-level AspNetCore request spans
        // for the MCP transport endpoint complete on a non-deterministic schedule
        // relative to the capture window. They carry no MCP/GraphQL information, so
        // drop them; the meaningful subgraph HTTP spans are nested and unaffected.
        if (capture["activities"] is JArray roots)
        {
            foreach (var root in roots
                .OfType<JObject>()
                .Where(a => (string?)a["OperationName"] == "Microsoft.AspNetCore.Hosting.HttpRequestIn")
                .ToList())
            {
                root.Remove();
            }
        }

        var json = capture.ToString(Formatting.Indented);

        // The MCP session id is randomly generated per run, scrub it so the snapshot is stable.
        json = SessionIdRegex().Replace(json, "$1<scrubbed>$2");
        json.MatchSnapshot();
    }

    private static PromptDefinition CreateGreetingPrompt()
        => new("greeting")
        {
            Title = "Greeting",
            Description = "A static greeting prompt.",
            Messages =
            [
                new PromptMessageDefinition(
                    RoleDefinition.User,
                    new TextContentBlockDefinition("Say hello to the world."))
            ]
        };

    private async Task<Gateway> CreateGatewayAsync(
        string? toolDocument = null,
        PromptDefinition? prompt = null)
    {
        // Ownership of the source-schema TestServer stays with the FusionTestBase
        // session, so it must not be disposed here.
        var sourceSchema = CreateSourceSchema("a", b => b.AddQueryType<Query>());
        var storage = new InMemoryMcpStorage(toolDocument, prompt);

        return await CreateCompositeSchemaAsync(
            [("a", sourceSchema)],
            configureApplication: app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints => endpoints.MapGraphQLMcp());
            },
            configureGatewayBuilder: b => b
                .AddInstrumentation()
                .AddMcp()
                .AddMcpStorage(storage));
    }

    private static async Task<McpClient> CreateMcpClientAsync(HttpClient httpClient)
    {
        return await McpClient.CreateAsync(
            new HttpClientTransport(
                new HttpClientTransportOptions
                {
                    Endpoint = new Uri(httpClient.BaseAddress!, "/graphql/mcp")
                },
                httpClient));
    }

    public class Query
    {
        public Book GetBook() => new("C# in Depth");

        public Book GetFaultyBook(IResolverContext context)
            => throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("Something went wrong")
                    .SetPath(context.Path)
                    .Build());
    }

    public record Book(string Title);

    private sealed class InMemoryMcpStorage(string? toolDocument, PromptDefinition? prompt) : IMcpStorage
    {
        private readonly List<OperationToolDefinition> _tools = toolDocument is null
            ? []
            : [new OperationToolDefinition(Utf8GraphQLParser.Parse(toolDocument))];

        private readonly List<PromptDefinition> _prompts = prompt is null ? [] : [prompt];

        public ValueTask<IEnumerable<OperationToolDefinition>> GetOperationToolDefinitionsAsync(
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult<IEnumerable<OperationToolDefinition>>(_tools);

        public ValueTask<IEnumerable<PromptDefinition>> GetPromptDefinitionsAsync(
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult<IEnumerable<PromptDefinition>>(_prompts);

        public IDisposable Subscribe(IObserver<OperationToolStorageEventArgs> observer)
            => NoopDisposable.Instance;

        public IDisposable Subscribe(IObserver<PromptStorageEventArgs> observer)
            => NoopDisposable.Instance;

        private sealed class NoopDisposable : IDisposable
        {
            public static readonly NoopDisposable Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
#endif
