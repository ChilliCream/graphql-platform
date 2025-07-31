using System.Text.Encodings.Web;
using System.Text.Json;
using CookieCrumble;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.ModelContextProtocol.Extensions;
using HotChocolate.ModelContextProtocol.Storage;
using HotChocolate.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;

namespace HotChocolate.ModelContextProtocol;

public sealed class IntegrationTests
{
    [Fact]
    public async Task ListTools_Valid_ReturnsTools()
    {
        // arrange
        var storage = new InMemoryMcpOperationDocumentStorage();
        await storage.SaveToolDocumentAsync(
            Utf8GraphQLParser.Parse(
                await File.ReadAllTextAsync("__resources__/GetWithNullableVariables.graphql")));
        await storage.SaveToolDocumentAsync(
            Utf8GraphQLParser.Parse(
                await File.ReadAllTextAsync("__resources__/GetWithNonNullableVariables.graphql")));
        var server =
            CreateTestServer(
                services => services.AddSingleton<IMcpOperationDocumentStorage>(storage));
        var mcpClient = await CreateMcpClient(server.CreateClient());

        // act
        var tools = await mcpClient.ListToolsAsync();

        // assert
        JsonSerializer.Serialize(
            tools.Select(
                t =>
                    new
                    {
                        t.Name,
                        t.Title,
                        t.Description
                    }),
                s_jsonSerializerOptions)
            .ReplaceLineEndings("\n")
            .MatchSnapshot(extension: ".json");
    }

    [Fact]
    public async Task CallTool_GetWithNullableVariables_ReturnsExpectedResult()
    {
        // arrange
        var storage = new InMemoryMcpOperationDocumentStorage();
        await storage.SaveToolDocumentAsync(
            Utf8GraphQLParser.Parse(
                await File.ReadAllTextAsync("__resources__/GetWithNullableVariables.graphql")));
        var server =
            CreateTestServer(
                services => services.AddSingleton<IMcpOperationDocumentStorage>(storage));
        var mcpClient = await CreateMcpClient(server.CreateClient());

        // act
        var result = await mcpClient.CallToolAsync(
            "get_with_nullable_variables",
            new Dictionary<string, object?>
            {
                { "any", null },
                { "boolean", null },
                { "byte", null },
                { "byteArray", null },
                { "date", null },
                { "dateTime", null },
                { "decimal", null },
                { "enum", null },
                { "float", null },
                { "id", null },
                { "int", null },
                { "json", null },
                { "list", null },
                { "localDate", null },
                { "localDateTime", null },
                { "localTime", null },
                { "long", null },
                { "object", null },
                { "short", null },
                { "string", null },
                { "timeSpan", null },
                { "unknown", null },
                { "url", null },
                { "uuid", null }
            },
            serializerOptions: JsonSerializerOptions.Default);

        // assert
        result.StructuredContent!
            .ToString()
            .ReplaceLineEndings("\n")
            .MatchSnapshot(extension: ".json");
    }

    [Fact]
    public async Task CallTool_GetWithNonNullableVariables_ReturnsExpectedResult()
    {
        // arrange
        var storage = new InMemoryMcpOperationDocumentStorage();
        await storage.SaveToolDocumentAsync(
            Utf8GraphQLParser.Parse(
                await File.ReadAllTextAsync("__resources__/GetWithNonNullableVariables.graphql")));
        var server =
            CreateTestServer(
                services => services.AddSingleton<IMcpOperationDocumentStorage>(storage));
        var mcpClient = await CreateMcpClient(server.CreateClient());

        // act
        var result = await mcpClient.CallToolAsync(
            "get_with_non_nullable_variables",
            // JSON values.
            new Dictionary<string, object?>
            {
                { "any", new { key = "value" } },
                { "boolean", true },
                { "byte", 1 },
                { "byteArray", "dGVzdA==" },
                { "date", "2000-01-01" },
                { "dateTime", "2000-01-01T12:00:00Z" },
                { "decimal", 79228162514264337593543950335m },
                { "enum", "VALUE1" },
                { "float", 1.5 },
                { "id", "test" },
                { "int", 1 },
                { "json", new { key = "value" } },
                { "list", s_list },
                { "localDate", "2000-01-01" },
                { "localDateTime", "2000-01-01T12:00:00" },
                { "localTime", "12:00:00" },
                { "long", 9223372036854775807 },
                { "object", new { field1A = new { field1B = new { field1C = "12:00:00" } } } },
                { "short", 1 },
                { "string", "test" },
                { "timeSpan", "PT5M" },
                { "unknown", "test" },
                { "url", "https://example.com" },
                { "uuid", "00000000-0000-0000-0000-000000000000" }
            },
            serializerOptions: JsonSerializerOptions.Default);

        // assert
        result.StructuredContent!
            .ToString()
            .ReplaceLineEndings("\n")
            .MatchSnapshot(extension: ".json");
    }

    [Fact]
    public async Task CallTool_GetWithDefaultedVariables_ReturnsExpectedResult()
    {
        // arrange
        var storage = new InMemoryMcpOperationDocumentStorage();
        await storage.SaveToolDocumentAsync(
            Utf8GraphQLParser.Parse(
                await File.ReadAllTextAsync("__resources__/GetWithDefaultedVariables.graphql")));
        var server =
            CreateTestServer(
                services => services.AddSingleton<IMcpOperationDocumentStorage>(storage));
        var mcpClient = await CreateMcpClient(server.CreateClient());

        // act
        var result = await mcpClient.CallToolAsync(
            "get_with_defaulted_variables",
            // FIXME: It should not be necessary to provide this variable.
            new Dictionary<string, object?>
            {
                { "list", s_list }
            });

        // assert
        result.StructuredContent!
            .ToString()
            .ReplaceLineEndings("\n")
            .MatchSnapshot(extension: ".json");
    }

    [Fact]
    public async Task CallTool_GetWithComplexVariables_ReturnsExpectedResult()
    {
        // arrange
        var storage = new InMemoryMcpOperationDocumentStorage();
        await storage.SaveToolDocumentAsync(
            Utf8GraphQLParser.Parse(
                await File.ReadAllTextAsync("__resources__/GetWithComplexVariables.graphql")));
        var server =
            CreateTestServer(
                configureServices: s => s.AddSingleton<IMcpOperationDocumentStorage>(storage),
                configureRequestExecutor: e => e.AddType(new TimeSpanType(TimeSpanFormat.DotNet)));
        var mcpClient = await CreateMcpClient(server.CreateClient());

        // act
        var result = await mcpClient.CallToolAsync(
            "get_with_complex_variables",
            // JSON values.
            new Dictionary<string, object?>
            {
                {
                    "list",
                    new[]
                    {
                        new { field1A = new { field1B = new[] { new { field1C = "12:00:00" } } } }
                    }
                },
                {
                    "object",
                    new { field1A = new { field1B = new[] { new { field1C = "12:00:00" } } } }
                },
                { "nullDefault", null },
                { "listWithNullDefault", new string?[] { null } },
                {
                    "objectWithNullDefault",
                    new { field1A = new { field1B = new[] { new { field1C = (string?)null } } } }
                },
                { "oneOf", new { field1 = 1 } },
                { "oneOfList", new object[] { new { field1 = 1 }, new { field2 = "test" } } },
                { "objectWithOneOfField", new { field = new { field1 = 1 } } },
                { "timeSpanDotNet", "00:05:00" }
            },
            serializerOptions: JsonSerializerOptions.Default);

        // assert
        result.StructuredContent!
            .ToString()
            .ReplaceLineEndings("\n")
            .MatchSnapshot(extension: ".json");
    }

    private static readonly JsonSerializerOptions s_jsonSerializerOptions =
        new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };

    private static readonly string[] s_list = ["test"];

    private static TestServer CreateTestServer(
        Action<IServiceCollection>? configureServices = null,
        Action<IRequestExecutorBuilder>? configureRequestExecutor = null)
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(
                services =>
                {
                    configureServices?.Invoke(services);

                    var executor =
                        services
                            .AddRouting()
                            .AddGraphQL()
                            .AddMcp()
                            .AddQueryType<TestSchema.Query>();

                    configureRequestExecutor?.Invoke(executor);

                    services
                        .AddMcpServer()
                        .WithHttpTransport()
                        .WithGraphQLTools();
                })
            .Configure(
                app =>
                {
                    app
                        .UseRouting()
                        .UseEndpoints(endpoints => endpoints.MapMcp("/mcp"));
                });

        return new TestServer(builder);
    }

    private static async Task<IMcpClient> CreateMcpClient(HttpClient httpClient)
    {
        return
            await McpClientFactory.CreateAsync(
                new SseClientTransport(
                    new SseClientTransportOptions
                    {
                        Endpoint = new Uri(httpClient.BaseAddress!, "/mcp")
                    },
                    httpClient));
    }
}
