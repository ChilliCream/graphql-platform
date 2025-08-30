using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.ModelContextProtocol.Extensions;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HotChocolate.ModelContextProtocol;

public sealed class IntegrationTests
{
    [Fact]
    public async Task ListTools_Valid_ReturnsTools()
    {
        // arrange
        var storage = new TestOperationToolStorage();
        await storage.AddOrUpdateToolAsync(
            Utf8GraphQLParser.Parse(
                await File.ReadAllTextAsync("__resources__/GetWithNullableVariables.graphql")));
        await storage.AddOrUpdateToolAsync(
            Utf8GraphQLParser.Parse(
                await File.ReadAllTextAsync("__resources__/GetWithNonNullableVariables.graphql")));
        var server = CreateTestServer(b => b.AddMcpToolStorage(storage));
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
        var storage = new TestOperationToolStorage();
        await storage.AddOrUpdateToolAsync(
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
                s_jsonSerializerOptions)
            .ReplaceLineEndings("\n")
            .MatchSnapshot(extension: ".json");
        Assert.True(secondClientNotified);
    }

    [Fact]
    public async Task ListTools_AfterToolsUpdate_ReturnsUpdatedTools()
    {
        // arrange
        var storage = new TestOperationToolStorage();
        await storage.AddOrUpdateToolAsync(
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
                    .AddMcpToolStorage(storage))
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
        IList<McpClientTool>? updatedTools = null;

        await storage.AddOrUpdateToolAsync(
            Utf8GraphQLParser.Parse(
                await File.ReadAllTextAsync("__resources__/GetSingleFieldWithAlias.graphql")));

        if (listChangedResetEvent.Wait(TimeSpan.FromSeconds(5)))
        {
            updatedTools = await mcpClient1.ListToolsAsync();
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
    public async Task ListTools_WithCustomTool_ReturnsExpectedResult()
    {
        // arrange
        var storage = new TestOperationToolStorage();
        await storage.AddOrUpdateToolAsync(
            Utf8GraphQLParser.Parse("query GetBooks { books { title } }"));
        var server =
            CreateTestServer(
                configureRequestExecutor: b => b.AddMcpToolStorage(storage),
                configureMcpServer: b => b.WithTools([typeof(TestTool)]));
        var mcpClient = await CreateMcpClientAsync(server.CreateClient());

        // act
        var tools = await mcpClient.ListToolsAsync();

        // assert
        Assert.Equal("get_books", tools[0].Name);
        Assert.Equal("test", tools[1].Name);
    }

    [Fact]
    public async Task CallTool_GetWithNullableVariables_ReturnsExpectedResult()
    {
        // arrange
        var storage = new TestOperationToolStorage();
        await storage.AddOrUpdateToolAsync(
            Utf8GraphQLParser.Parse(
                await File.ReadAllTextAsync("__resources__/GetWithNullableVariables.graphql")));
        var server = CreateTestServer(b => b.AddMcpToolStorage(storage));
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
        var storage = new TestOperationToolStorage();
        await storage.AddOrUpdateToolAsync(
            Utf8GraphQLParser.Parse(
                await File.ReadAllTextAsync("__resources__/GetWithNonNullableVariables.graphql")));
        var server = CreateTestServer(b => b.AddMcpToolStorage(storage));
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
        var storage = new TestOperationToolStorage();
        await storage.AddOrUpdateToolAsync(
            Utf8GraphQLParser.Parse(
                await File.ReadAllTextAsync("__resources__/GetWithDefaultedVariables.graphql")));
        var server = CreateTestServer(b => b.AddMcpToolStorage(storage));
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
        var storage = new TestOperationToolStorage();
        await storage.AddOrUpdateToolAsync(
            Utf8GraphQLParser.Parse(
                await File.ReadAllTextAsync("__resources__/GetWithComplexVariables.graphql")));
        var server =
            CreateTestServer(
                b => b
                    .AddMcpToolStorage(storage)
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
        var storage = new TestOperationToolStorage();
        await storage.AddOrUpdateToolAsync(
            Utf8GraphQLParser.Parse("query GetWithErrors { withErrors }"));
        var server = CreateTestServer(b => b.AddMcpToolStorage(storage));
        var mcpClient = await CreateMcpClientAsync(server.CreateClient());

        // act
        var result = await mcpClient.CallToolAsync("get_with_errors");

        // assert
        result.StructuredContent!
            .ToString()
            .ReplaceLineEndings("\n")
            .MatchSnapshot(extension: ".json");
    }

    [Fact]
    public async Task CallTool_GetWithAuthSuccess_ReturnsExpectedResult()
    {
        // arrange
        var storage = new TestOperationToolStorage();
        await storage.AddOrUpdateToolAsync(Utf8GraphQLParser.Parse("query GetWithAuth { withAuth }"));
        var server = CreateTestServer(b => b.AddMcpToolStorage(storage));
        var mcpClient = await CreateMcpClientAsync(server.CreateClient(), TestJwtTokenHelper.GenerateToken());

        // act
        var result1 = await mcpClient.CallToolAsync("get_with_auth");
        var result2 = await mcpClient.CallToolAsync("get_with_auth");

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result1.StructuredContent, "Result 1", markdownLanguage: "json");
        snapshot.Add(result2.StructuredContent, "Result 2", markdownLanguage: "json");
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task CallTool_GetWithAuthFailure_ReturnsExpectedResult()
    {
        // arrange
        var storage = new TestOperationToolStorage();
        await storage.AddOrUpdateToolAsync(Utf8GraphQLParser.Parse("query GetWithAuth { withAuth }"));
        var server = CreateTestServer(b => b.AddMcpToolStorage(storage));
        var mcpClient = await CreateMcpClientAsync(server.CreateClient());

        // act
        var result = await mcpClient.CallToolAsync("get_with_auth");

        // assert
        result.StructuredContent!
            .ToString()
            .ReplaceLineEndings("\n")
            .MatchSnapshot(extension: ".json");
    }

    [Fact]
    public async Task CallTool_WithCustomTool_ReturnsExpectedResult()
    {
        // arrange
        var server =
            CreateTestServer(
                configureRequestExecutor: b => b.AddMcpToolStorage(new TestOperationToolStorage()),
                configureMcpServer: b => b.WithTools([typeof(TestTool)]));
        var mcpClient = await CreateMcpClientAsync(server.CreateClient());

        // act
        var result = await mcpClient.CallToolAsync(
            "test",
            new Dictionary<string, object?>
            {
                { "message", "Hello, World!" }
            });

        // assert
        Assert.Equal("Hello, World!", ((TextContentBlock)result.Content[0]).Text);
    }

    [Fact]
    public async Task AddMcp_WithServerOption_SetsOption()
    {
        // arrange & act
        var server =
            CreateTestServer(
                configureRequestExecutor: b => b.AddMcpToolStorage(new TestOperationToolStorage()),
                configureMcpServerOptions: o => o.InitializationTimeout = TimeSpan.FromSeconds(10));
        await CreateMcpClientAsync(server.CreateClient());
        var executor = await server.Services.GetRequiredService<IRequestExecutorProvider>().GetExecutorAsync();
        var handler = executor.Schema.Services.GetRequiredService<StreamableHttpHandler>();
        var options = handler.Sessions.Values.First().Server!.ServerOptions;

        // assert
        Assert.Equal(TimeSpan.FromSeconds(10), options.InitializationTimeout);
    }

    private static readonly JsonSerializerOptions s_jsonSerializerOptions =
        new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };

    private static readonly string[] s_list = ["test"];

    private static TestServer CreateTestServer(
        Action<IRequestExecutorBuilder>? configureRequestExecutor = null,
        Action<McpServerOptions>? configureMcpServerOptions = null,
        Action<IMcpServerBuilder>? configureMcpServer = null)
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(
                services =>
                {
                    services
                        .AddAuthentication()
                        .AddJwtBearer(
                            o => o.TokenValidationParameters =
                                new TokenValidationParameters
                                {
                                    ValidIssuer = TokenIssuer,
                                    ValidAudience = TokenAudience,
                                    IssuerSigningKey = s_tokenKey
                                });

                    var executor =
                        services
                            .AddLogging()
                            .AddRouting()
                            .AddGraphQL()
                            .AddAuthorization()
                            .AddMcp(configureMcpServerOptions, configureMcpServer)
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
                    .UseAuthentication()
                    .UseEndpoints(endpoints => endpoints.MapGraphQLMcp()));

        return new TestServer(builder);
    }

    private static async Task<IMcpClient> CreateMcpClientAsync(HttpClient httpClient, string? token = null)
    {
        return
            await McpClientFactory.CreateAsync(
                new SseClientTransport(
                    new SseClientTransportOptions
                    {
                        Endpoint = new Uri(httpClient.BaseAddress!, "/graphql/mcp"),
                        AdditionalHeaders = new Dictionary<string, string>()
                        {
                            { "Authorization", $"Bearer {token}" }
                        }
                    },
                    httpClient));
    }

    [McpServerToolType]
    private static class TestTool
    {
        [McpServerTool]
        // ReSharper disable once UnusedMember.Local
        public static string Test(string message) => message;
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

    private static class TestJwtTokenHelper
    {
        public static string GenerateToken()
        {
            var claims = new Claim[]
            {
                new(ClaimTypes.Name, "Test"),
                new(ClaimTypes.Role, "Admin")
            };

            var token = new JwtSecurityToken(
                issuer: TokenIssuer,
                audience: TokenAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: new SigningCredentials(s_tokenKey, SecurityAlgorithms.HmacSha256));

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    private const string TokenIssuer = "test-issuer";
    private const string TokenAudience = "test-audience";
    private static readonly SymmetricSecurityKey s_tokenKey = new("test-secret-key-at-least-32-bytes"u8.ToArray());
}
