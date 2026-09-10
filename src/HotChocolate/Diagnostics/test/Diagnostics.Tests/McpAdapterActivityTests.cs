#if NET9_0_OR_GREATER
using System.Text.RegularExpressions;
using HotChocolate.Adapters.Mcp.Storage;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using static CookieCrumble.TestEnvironment;
using static HotChocolate.Diagnostics.ActivityTestHelper;

namespace HotChocolate.Diagnostics;

[Collection("Instrumentation")]
public partial class McpAdapterActivityTests
{
    [Fact]
    public async Task Mcp_CallTool()
    {
        // arrange
        using var server = CreateServer("query GetBook { book { title } }");
        var mcpClient = await CreateMcpClientAsync(server.CreateClient());
        await mcpClient.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        using (CaptureActivities(out var activities))
        {
            // act
            await mcpClient.CallToolAsync("get_book", cancellationToken: TestContext.Current.CancellationToken);

            // assert
            MatchActivitySnapshot(activities);
        }
    }

    [Fact]
    public async Task Mcp_CallTool_Field_Does_Not_Exist()
    {
        // arrange
        using var server = CreateServer("query InvalidGraphqlQuery { doesNotExist }");
        var mcpClient = await CreateMcpClientAsync(server.CreateClient());
        await mcpClient.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        using (CaptureActivities(out var activities))
        {
            // act
            await mcpClient.CallToolAsync(
                "invalid_graphql_query",
                cancellationToken: TestContext.Current.CancellationToken);

            // assert
            MatchActivitySnapshot(activities);
        }
    }

    [Fact]
    public async Task Mcp_CallTool_Tool_Does_Not_Exist()
    {
        // arrange
        using var server = CreateServer("query GetBook { book { title } }");
        var mcpClient = await CreateMcpClientAsync(server.CreateClient());
        await mcpClient.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        using (CaptureActivities(out var activities))
        {
            // act
            await mcpClient.CallToolAsync("does_not_exist", cancellationToken: TestContext.Current.CancellationToken);

            // assert
            MatchActivitySnapshot(activities);
        }
    }

    [Fact]
    public async Task Mcp_CallTool_GraphQL_Field_Error()
    {
        // arrange
        using var server = CreateServer("query GetFaultyBook { faultyBook { title } }");
        var mcpClient = await CreateMcpClientAsync(server.CreateClient());
        await mcpClient.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        using (CaptureActivities(out var activities))
        {
            // act
            await mcpClient.CallToolAsync("get_faulty_book", cancellationToken: TestContext.Current.CancellationToken);

            // assert
            MatchActivitySnapshot(activities);
        }
    }

    [Fact]
    public async Task Mcp_GetPrompt()
    {
        // arrange
        using var server = CreateServer(prompt: CreateGreetingPrompt());
        var mcpClient = await CreateMcpClientAsync(server.CreateClient());
        await mcpClient.ListPromptsAsync(cancellationToken: TestContext.Current.CancellationToken);

        using (CaptureActivities(out var activities))
        {
            // act
            await mcpClient.GetPromptAsync("greeting", cancellationToken: TestContext.Current.CancellationToken);

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
        // relative to the capture window, and their terminal status and tags vary
        // with whether the connection has been torn down. They carry no MCP or
        // GraphQL information, so drop them; only these top-level transport spans are
        // removed and every other captured span is unaffected.
        // Keep in sync with HotChocolate.Fusion.Diagnostics.FusionMcpAdapterActivityTests.
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
        json.MatchSnapshot(Postfix([NET11_0]));
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

    private static TestServer CreateServer(string? toolDocument = null, PromptDefinition? prompt = null)
    {
        var storage = new InMemoryMcpStorage(toolDocument, prompt);

        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services
                    .AddGraphQLServer()
                    .AddInstrumentation()
                    .AddMcp()
                    .AddMcpStorage(storage)
                    .AddQueryType<Query>();
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints => endpoints.MapGraphQLMcp());
            });

        return new TestServer(builder);
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
