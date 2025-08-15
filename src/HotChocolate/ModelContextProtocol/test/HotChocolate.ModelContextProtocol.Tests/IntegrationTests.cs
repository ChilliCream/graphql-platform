using System.Text.Encodings.Web;
using System.Text.Json;
using CookieCrumble;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.ModelContextProtocol.Extensions;
using HotChocolate.ModelContextProtocol.Storage;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

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
        var server = CreateTestServer(b => b.AddMcpOperationDocumentStorage(storage));
        var mcpClient = await CreateMcpClientAsync(server.CreateClient());

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
    public async Task ListTools_AfterSchemaUpdate_ReturnsUpdatedTools()
    {
        // arrange
        var storage = new InMemoryMcpOperationDocumentStorage();
        await storage.SaveToolDocumentAsync(
            Utf8GraphQLParser.Parse(
                await File.ReadAllTextAsync("__resources__/GetSingleField.graphql")));
        var typeModule = new TestTypeModule();
        var builder = new WebHostBuilder()
            .ConfigureServices(
                services => services
                    .AddRouting()
                    .AddGraphQL()
                    .AddTypeModule(_ => typeModule)
                    .AddMcp()
                    .AddMcpOperationDocumentStorage(storage))
            .Configure(
                app => app
                    .UseRouting()
                    .UseEndpoints(endpoints => endpoints.MapGraphQLMcp()));
        var server = new TestServer(builder);
        var mcpClient1 = await CreateMcpClientAsync(server.CreateClient());
        var listChangedResetEvent = new ManualResetEventSlim(false);
        mcpClient1.RegisterNotificationHandler(
            NotificationMethods.ToolListChangedNotification,
            async (_, _) =>
            {
                listChangedResetEvent.Set();
                await ValueTask.CompletedTask;
            });

        // act
        var tools = await mcpClient1.ListToolsAsync();
        typeModule.TriggerChange();
        IList<McpClientTool>? updatedTools = null;

        if (listChangedResetEvent.Wait(TimeSpan.FromSeconds(5)))
        {
            var mcpClient2 = await CreateMcpClientAsync(server.CreateClient());
            updatedTools = await mcpClient2.ListToolsAsync();
        }

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
        var server = CreateTestServer(b => b.AddMcpOperationDocumentStorage(storage));
        var mcpClient = await CreateMcpClientAsync(server.CreateClient());

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
        var server = CreateTestServer(b => b.AddMcpOperationDocumentStorage(storage));
        var mcpClient = await CreateMcpClientAsync(server.CreateClient());

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
        var server = CreateTestServer(b => b.AddMcpOperationDocumentStorage(storage));
        var mcpClient = await CreateMcpClientAsync(server.CreateClient());

        // act
        var result = await mcpClient.CallToolAsync("get_with_defaulted_variables");

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
                b => b
                    .AddMcpOperationDocumentStorage(storage)
                    .AddType(new TimeSpanType(TimeSpanFormat.DotNet)));
        var mcpClient = await CreateMcpClientAsync(server.CreateClient());

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

    [Fact]
    public async Task CallTool_GetWithErrors_ReturnsExpectedResult()
    {
        // arrange
        var storage = new InMemoryMcpOperationDocumentStorage();
        await storage.SaveToolDocumentAsync(
            Utf8GraphQLParser.Parse("query GetWithErrors { withErrors }"));
        var server = CreateTestServer(b => b.AddMcpOperationDocumentStorage(storage));
        var mcpClient = await CreateMcpClientAsync(server.CreateClient());

        // act
        var result = await mcpClient.CallToolAsync("get_with_errors");

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
        Action<IRequestExecutorBuilder>? configureRequestExecutor = null)
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(
                services =>
                {
                    var executor =
                        services
                            .AddRouting()
                            .AddGraphQL()
                            .AddMcp()
                            .AddQueryType<TestSchema.Query>()
                            .AddInterfaceType<TestSchema.IPet>()
                            .AddUnionType<TestSchema.IPet>()
                            .AddObjectType<TestSchema.Cat>()
                            .AddObjectType<TestSchema.Dog>();

                    configureRequestExecutor?.Invoke(executor);
                })
            .Configure(
                app => app
                    .UseRouting()
                    .UseEndpoints(endpoints => endpoints.MapGraphQLMcp()));

        return new TestServer(builder);
    }

    private static async Task<IMcpClient> CreateMcpClientAsync(HttpClient httpClient)
    {
        return
            await McpClientFactory.CreateAsync(
                new SseClientTransport(
                    new SseClientTransportOptions
                    {
                        Endpoint = new Uri(httpClient.BaseAddress!, "/graphql/mcp")
                    },
                    httpClient));
    }

    private class TestTypeModule : TypeModule
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
