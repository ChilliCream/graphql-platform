using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using HotChocolate.Adapters.Mcp.Diagnostics;
using HotChocolate.Adapters.Mcp.Storage;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HotChocolate.Adapters.Mcp;

public abstract class IntegrationTestBase
{
    [Fact]
    public async Task ListPrompts_Valid_ReturnsPrompts()
    {
        // arrange
        var storage = new TestMcpStorage();
        await storage.AddOrUpdatePromptAsync(
            new PromptDefinition("code_review")
            {
                Title = "Code Review",
                Description = "Asks the LLM to analyze code quality and suggest improvements.",
                Arguments =
                [
                    new PromptArgumentDefinition("code")
                    {
                        Title = "Code to Review",
                        Description = "The code to review",
                        Required = true
                    }
                ],
                Icons =
                [
                    new IconDefinition(new Uri("https://example.com/icon.png"))
                    {
                        MimeType = "image/png",
                        Sizes = ["48x48"],
                        Theme = "light"
                    }
                ],
                Messages =
                [
                    new PromptMessageDefinition(
                        RoleDefinition.User,
                        new TextContentBlockDefinition(
                            """
                            Please review this code:

                            ```
                            {code}
                            ```
                            """))
                ]
            });
        await storage.AddOrUpdatePromptAsync(
            new PromptDefinition("bug_finder")
            {
                Title = "Bug Finder",
                Description = "Asks the LLM to find bugs in the provided code.",
                Arguments =
                [
                    new PromptArgumentDefinition("code")
                    {
                        Title = "Code to Analyze",
                        Description = "The code to analyze for bugs",
                        Required = true
                    }
                ],
                Messages =
                [
                    new PromptMessageDefinition(
                        RoleDefinition.User,
                        new TextContentBlockDefinition(
                            """
                            Please find bugs in this code:

                            ```
                            {code}
                            ```
                            """))
                ]
            });
        var server = await CreateTestServerAsync(storage);
        var mcpClient = await CreateMcpClientAsync(server.CreateClient());

        // act
        var prompts = await mcpClient.ListPromptsAsync();

        // assert
        JsonSerializer.Serialize(prompts.Select(t => t.ProtocolPrompt), JsonSerializerOptions)
            .ReplaceLineEndings("\n")
            .MatchSnapshot(extension: ".json");
    }

    [Fact]
    public async Task ListPrompts_AfterPromptsUpdate_ReturnsUpdatedPrompts()
    {
        // arrange
        var storage = new TestMcpStorage();
        await storage.AddOrUpdatePromptAsync(
            new PromptDefinition("code_review")
            {
                Title = "Code Review",
                Description = "Asks the LLM to analyze code quality and suggest improvements.",
                Arguments =
                [
                    new PromptArgumentDefinition("code")
                    {
                        Title = "Code to Review",
                        Description = "The code to review",
                        Required = true
                    }
                ],
                Icons =
                [
                    new IconDefinition(new Uri("https://example.com/icon.png"))
                    {
                        MimeType = "image/png",
                        Sizes = ["48x48"],
                        Theme = "light"
                    }
                ],
                Messages =
                [
                    new PromptMessageDefinition(
                        RoleDefinition.User,
                        new TextContentBlockDefinition(
                            """
                            Please review this code:

                            ```
                            {code}
                            ```
                            """))
                ]
            });
        var server = await CreateTestServerAsync(storage);
        var mcpClient = await CreateMcpClientAsync(server.CreateClient());
        var listChangedResetEvent = new ManualResetEventSlim(false);
        mcpClient.RegisterNotificationHandler(
            NotificationMethods.PromptListChangedNotification,
            async (_, _) =>
            {
                listChangedResetEvent.Set();
                await ValueTask.CompletedTask;
            });

        // act
        var prompts = await mcpClient.ListPromptsAsync();
        IList<McpClientPrompt>? updatedPrompts = null;

        await storage.AddOrUpdatePromptAsync(
            new PromptDefinition("bug_finder")
            {
                Title = "Bug Finder",
                Description = "Asks the LLM to find bugs in the provided code.",
                Arguments =
                [
                    new PromptArgumentDefinition("code")
                    {
                        Title = "Code to Analyze",
                        Description = "The code to analyze for bugs",
                        Required = true
                    }
                ],
                Messages =
                [
                    new PromptMessageDefinition(
                        RoleDefinition.User,
                        new TextContentBlockDefinition(
                            """
                            Please find bugs in this code:

                            ```
                            {code}
                            ```
                            """))
                ]
            });

        if (listChangedResetEvent.Wait(TimeSpan.FromSeconds(5)))
        {
            updatedPrompts = await mcpClient.ListPromptsAsync();
        }

        // assert
        Assert.NotNull(updatedPrompts);
        JsonSerializer.Serialize(
                prompts.Concat(updatedPrompts).Select(t => t.ProtocolPrompt),
                JsonSerializerOptions)
            .ReplaceLineEndings("\n")
            .MatchSnapshot(extension: ".json");
    }

    [Fact]
    public async Task GetPrompt_Valid_ReturnsPrompt()
    {
        // arrange
        var storage = new TestMcpStorage();
        var prompt =
            new PromptDefinition("code_review")
            {
                Title = "Code Review",
                Description = "Asks the LLM to analyze code quality and suggest improvements.",
                Arguments =
                [
                    new PromptArgumentDefinition("code")
                    {
                        Title = "Code to Review",
                        Description = "The code to review",
                        Required = true
                    }
                ],
                Icons =
                [
                    new IconDefinition(new Uri("https://example.com/icon.png"))
                    {
                        MimeType = "image/png",
                        Sizes = ["48x48"],
                        Theme = "light"
                    }
                ],
                Messages =
                [
                    new PromptMessageDefinition(
                        RoleDefinition.User,
                        new TextContentBlockDefinition(
                            """
                            Please review this code:

                            ```
                            {code}
                            ```
                            """))
                ]
            };
        await storage.AddOrUpdatePromptAsync(prompt);
        var server = await CreateTestServerAsync(storage);
        var mcpClient = await CreateMcpClientAsync(server.CreateClient());

        // act
        var result = await mcpClient.GetPromptAsync(
            prompt.Name,
            new Dictionary<string, object?>
            {
                {
                    "code",
                    """
                    // Example code.
                    console.log("Hello, World!");
                    """
                }
            });

        // assert
        JsonSerializer.Serialize(result, JsonSerializerOptions)
            .ReplaceLineEndings("\n")
            .MatchSnapshot(extension: ".json");
    }

    [Fact]
    public async Task GetPrompt_Missing_ThrowsException()
    {
        // arrange
        var storage = new TestMcpStorage();
        var server = await CreateTestServerAsync(storage);
        var mcpClient = await CreateMcpClientAsync(server.CreateClient());

        // act
        async Task Action() => await mcpClient.GetPromptAsync("missing");

        // assert
        var exception = await Assert.ThrowsAsync<McpProtocolException>(Action);
        Assert.EndsWith("Prompt not found.", exception.Message);
        Assert.Equal(-32602, (int)exception.ErrorCode);
        Assert.Equal("missing", exception.Data["name"]);
    }

    [Fact]
    public async Task ListTools_Valid_ReturnsTools()
    {
        // arrange
        var storage = new TestMcpStorage();
        await storage.AddOrUpdateToolAsync(
            new OperationToolDefinition(
                Utf8GraphQLParser.Parse(
                    await File.ReadAllTextAsync("__resources__/GetWithNullableVariables.graphql"))));
        await storage.AddOrUpdateToolAsync(
            new OperationToolDefinition(
                Utf8GraphQLParser.Parse(
                    await File.ReadAllTextAsync("__resources__/GetWithNonNullableVariables.graphql"))));
        var server = await CreateTestServerAsync(storage);
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
                JsonSerializerOptions)
            .ReplaceLineEndings("\n")
            .MatchSnapshot(extension: ".json");
    }

    [Fact]
    public async Task ListTools_AfterToolsUpdate_ReturnsUpdatedTools()
    {
        // arrange
        var storage = new TestMcpStorage();
        await storage.AddOrUpdateToolAsync(
            new OperationToolDefinition(
                Utf8GraphQLParser.Parse(
                    await File.ReadAllTextAsync("__resources__/GetBooksWithTitle1.graphql"))));
        var server = await CreateTestServerAsync(storage);
        var mcpClient = await CreateMcpClientAsync(server.CreateClient());
        var listChangedResetEvent = new ManualResetEventSlim(false);
        mcpClient.RegisterNotificationHandler(
            NotificationMethods.ToolListChangedNotification,
            async (_, _) =>
            {
                listChangedResetEvent.Set();
                await ValueTask.CompletedTask;
            });

        // act
        var tools = await mcpClient.ListToolsAsync();
        IList<McpClientTool>? updatedTools = null;

        await storage.AddOrUpdateToolAsync(
            new OperationToolDefinition(
                Utf8GraphQLParser.Parse(
                    await File.ReadAllTextAsync("__resources__/GetBooksWithTitle2.graphql"))));

        if (listChangedResetEvent.Wait(TimeSpan.FromSeconds(5)))
        {
            updatedTools = await mcpClient.ListToolsAsync();
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
                JsonSerializerOptions)
            .ReplaceLineEndings("\n")
            .MatchSnapshot(extension: ".json");
    }

    [Fact]
    public async Task ListTools_WithCustomTool_ReturnsExpectedResult()
    {
        // arrange
        var storage = new TestMcpStorage();
        await storage.AddOrUpdateToolAsync(
            new OperationToolDefinition(
                Utf8GraphQLParser.Parse("query GetBooks { books { title } }")));
        var server =
            await CreateTestServerAsync(
                storage,
                configureMcpServer: b => b.WithTools([typeof(TestTool)]));
        var mcpClient = await CreateMcpClientAsync(server.CreateClient());

        // act
        var tools = await mcpClient.ListToolsAsync();

        // assert
        Assert.Equal("get_books", tools[0].Name);
        Assert.Equal("test", tools[1].Name);
    }

    [Fact]
    public async Task ListTools_WithMcpAppView_ReturnsExpectedResult()
    {
        // arrange
        var storage = new TestMcpStorage();
        var document1 =
            Utf8GraphQLParser.Parse(
                await File.ReadAllTextAsync("__resources__/GetWithNullableVariables.graphql"));
        var document2 =
            Utf8GraphQLParser.Parse(
                await File.ReadAllTextAsync("__resources__/GetWithNonNullableVariables.graphql"));
        await storage.AddOrUpdateToolAsync(new OperationToolDefinition(document1));
        await storage.AddOrUpdateToolAsync(
            new OperationToolDefinition(document2)
            {
                View = new McpAppView(
                    html: await File.ReadAllTextAsync("__resources__/McpAppView.html"))
            });
        var server = await CreateTestServerAsync(storage);
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
                            t.Description,
                            t.ProtocolTool.Meta
                        }),
                JsonSerializerOptions)
            .ReplaceLineEndings("\n")
            .MatchSnapshot(extension: ".json");
    }

    [Fact]
    public async Task ListTools_SetTitle_ReturnsExpectedResult()
    {
        // arrange
        var storage = new TestMcpStorage();
        await storage.AddOrUpdateToolAsync(
            new OperationToolDefinition(
                Utf8GraphQLParser.Parse(
                    """
                    query GetBooks {
                        books {
                            title
                        }
                    }
                    """))
            {
                Title = "Custom Title"
            });
        var server = await CreateTestServerAsync(storage);
        var mcpClient = await CreateMcpClientAsync(server.CreateClient());

        // act
        var tools = await mcpClient.ListToolsAsync();

        // assert
        Assert.Equal("Custom Title", tools[0].Title);
    }

    [Fact]
    public async Task ListTools_SetIcons_ReturnsExpectedResult()
    {
        // arrange
        var storage = new TestMcpStorage();
        await storage.AddOrUpdateToolAsync(
            new OperationToolDefinition(
                Utf8GraphQLParser.Parse(
                    """
                    query GetBooks {
                        books {
                            title
                        }
                    }
                    """))
            {
                Icons =
                [
                    new IconDefinition(new Uri("https://example.com/icon.png"))
                    {
                        MimeType = "image/png",
                        Sizes = ["48x48"],
                        Theme = "light"
                    },
                    new IconDefinition(new Uri("data:image/svg+xml;base64,..."))
                    {
                        MimeType = "image/svg+xml",
                        Sizes = ["any"],
                        Theme = "dark"
                    }
                ]
            });
        var server = await CreateTestServerAsync(storage);
        var mcpClient = await CreateMcpClientAsync(server.CreateClient());

        // act
        var tools = await mcpClient.ListToolsAsync();
        var icons = tools[0].ProtocolTool.Icons;

        // assert
        Assert.Equal(2, icons?.Count);
        Assert.Equal("https://example.com/icon.png", icons?[0].Source);
        Assert.Equal("image/png", icons?[0].MimeType);
        Assert.Equal(["48x48"], icons?[0].Sizes);
        Assert.Equal("light", icons?[0].Theme);
        Assert.Equal("data:image/svg+xml;base64,...", icons?[1].Source);
        Assert.Equal("image/svg+xml", icons?[1].MimeType);
        Assert.Equal(["any"], icons?[1].Sizes);
        Assert.Equal("dark", icons?[1].Theme);
    }

    [Fact]
    public async Task ListTools_SetAnnotations_ReturnsExpectedResult()
    {
        // arrange
        var storage = new TestMcpStorage();
        await storage.AddOrUpdateToolAsync(
            new OperationToolDefinition(
                Utf8GraphQLParser.Parse(
                    """
                    mutation AddBook {
                        addBook { title }
                    }
                    """))
            {
                DestructiveHint = false,
                IdempotentHint = true,
                OpenWorldHint = false
            });
        var server = await CreateTestServerAsync(storage);
        var mcpClient = await CreateMcpClientAsync(server.CreateClient());

        // act
        var tools = await mcpClient.ListToolsAsync();

        // assert
        Assert.Equal(false, tools[0].ProtocolTool.Annotations?.DestructiveHint);
        Assert.Equal(true, tools[0].ProtocolTool.Annotations?.IdempotentHint);
        Assert.Equal(false, tools[0].ProtocolTool.Annotations?.OpenWorldHint);
    }

    [Fact]
    public async Task ListTools_SetAnnotationsInSchema_ReturnsExpectedResult()
    {
        // arrange
        var storage = new TestMcpStorage();
        await storage.AddOrUpdateToolAsync(
            new OperationToolDefinition(
                Utf8GraphQLParser.Parse(
                    await File.ReadAllTextAsync("__resources__/ExplicitNonDestructiveTool.graphql"))));
        await storage.AddOrUpdateToolAsync(
            new OperationToolDefinition(
                Utf8GraphQLParser.Parse(
                    await File.ReadAllTextAsync("__resources__/ExplicitIdempotentTool.graphql"))));
        await storage.AddOrUpdateToolAsync(
            new OperationToolDefinition(
                Utf8GraphQLParser.Parse(
                    await File.ReadAllTextAsync("__resources__/ExplicitClosedWorldTool.graphql"))));
        var server = await CreateTestServerAsync(storage);
        var mcpClient = await CreateMcpClientAsync(server.CreateClient());

        // act
        var tools = await mcpClient.ListToolsAsync();

        // assert
        Assert.Equal(false, tools[0].ProtocolTool.Annotations?.DestructiveHint);
        Assert.Equal(true, tools[0].ProtocolTool.Annotations?.IdempotentHint);
        Assert.Equal(false, tools[0].ProtocolTool.Annotations?.OpenWorldHint);
    }

    [Fact]
    public async Task ListTools_InitializeToolsInvalidDocument_ReturnsExpectedResult()
    {
        // arrange
        var storage = new TestMcpStorage();
        await storage.AddOrUpdateToolAsync(
            new OperationToolDefinition(
                Utf8GraphQLParser.Parse("query Invalid { doesNotExist1, doesNotExist2 }")));
        await storage.AddOrUpdateToolAsync(
            new OperationToolDefinition(
                Utf8GraphQLParser.Parse("query Valid { books { title } }")));
        var listener = new TestMcpDiagnosticEventListener();
        var server = await CreateTestServerAsync(storage, diagnosticEventListener: listener);
        var mcpClient = await CreateMcpClientAsync(server.CreateClient());

        // act
        var result = await mcpClient.ListToolsAsync();

        // assert
        Assert.Single(result, tool => tool.Name == "valid"); // The invalid tool is ignored.
        Assert.Collection(
            listener.ValidationErrorLog,
            firstError =>
                Assert.Equal("The field `doesNotExist1` does not exist on the type `Query`.", firstError.Message),
            secondError =>
                Assert.Equal("The field `doesNotExist2` does not exist on the type `Query`.", secondError.Message));
    }

    [Fact]
    public async Task ListTools_UpdateToolsInvalidDocument_ReturnsExpectedResult()
    {
        // arrange
        var storage = new TestMcpStorage();
        await storage.AddOrUpdateToolAsync(
            new OperationToolDefinition(Utf8GraphQLParser.Parse("query Tool { books { title } }"))
            {
                Title = "BEFORE"
            });
        var listener = new TestMcpDiagnosticEventListener();
        var server = await CreateTestServerAsync(storage, diagnosticEventListener: listener);
        var mcpClient = await CreateMcpClientAsync(server.CreateClient());

        // act
        await storage.AddOrUpdateToolAsync(
            new OperationToolDefinition(
                Utf8GraphQLParser.Parse("query Tool { doesNotExist1, doesNotExist2 }"))
            {
                Title = "AFTER"
            });
        await Task.Delay(1000); // Wait for the observer buffer to flush.
        var result = await mcpClient.ListToolsAsync();

        // assert
        Assert.Single(result, tool => tool.Title == "BEFORE"); // The invalid update is ignored.
        Assert.Collection(
            listener.ValidationErrorLog,
            firstError =>
                Assert.Equal("The field `doesNotExist1` does not exist on the type `Query`.", firstError.Message),
            secondError =>
                Assert.Equal("The field `doesNotExist2` does not exist on the type `Query`.", secondError.Message));
    }

    [Fact]
    public async Task CallTool_GetWithNullableVariables_ReturnsExpectedResult()
    {
        // arrange
        var storage = new TestMcpStorage();
        await storage.AddOrUpdateToolAsync(
            new OperationToolDefinition(
                Utf8GraphQLParser.Parse(
                    await File.ReadAllTextAsync("__resources__/GetWithNullableVariables.graphql"))));
        var server = await CreateTestServerAsync(storage);
        var mcpClient = await CreateMcpClientAsync(server.CreateClient());

        // act
        var result = await mcpClient.CallToolAsync(
            "get_with_nullable_variables",
            new Dictionary<string, object?>
            {
                { "any", null },
                { "base64String", null },
                { "boolean", null },
                { "byte", null },
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
                { "uri", null },
                { "url", null },
                { "uuid", null }
            },
            options: new RequestOptions { JsonSerializerOptions = JsonSerializerOptions.Default });

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
        var storage = new TestMcpStorage();
        await storage.AddOrUpdateToolAsync(
            new OperationToolDefinition(
                Utf8GraphQLParser.Parse(
                    await File.ReadAllTextAsync("__resources__/GetWithNonNullableVariables.graphql"))));
        var server = await CreateTestServerAsync(storage);
        var mcpClient = await CreateMcpClientAsync(server.CreateClient());

        // act
        var result = await mcpClient.CallToolAsync(
            "get_with_non_nullable_variables",
            // JSON values.
            new Dictionary<string, object?>
            {
                { "any", new { key = "value" } },
                { "base64String", "dGVzdA==" },
                { "boolean", true },
                { "byte", 1 },
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
                { "uri", "https://example.com" },
                { "url", "https://example.com" },
                { "uuid", "00000000-0000-0000-0000-000000000000" }
            },
            options: new RequestOptions { JsonSerializerOptions = JsonSerializerOptions.Default });

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
        var storage = new TestMcpStorage();
        await storage.AddOrUpdateToolAsync(
            new OperationToolDefinition(
                Utf8GraphQLParser.Parse(
                    await File.ReadAllTextAsync("__resources__/GetWithDefaultedVariables.graphql"))));
        var server = await CreateTestServerAsync(storage);
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
        var storage = new TestMcpStorage();
        await storage.AddOrUpdateToolAsync(
            new OperationToolDefinition(
                Utf8GraphQLParser.Parse(
                    await File.ReadAllTextAsync("__resources__/GetWithComplexVariables.graphql"))));
        var server = await CreateTestServerAsync(storage, [new TimeSpanType(TimeSpanFormat.DotNet)]);
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
            options: new RequestOptions { JsonSerializerOptions = JsonSerializerOptions.Default });

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
        var storage = new TestMcpStorage();
        await storage.AddOrUpdateToolAsync(
            new OperationToolDefinition(
                Utf8GraphQLParser.Parse("query GetWithErrors { withErrors }")));
        var server = await CreateTestServerAsync(storage);
        var mcpClient = await CreateMcpClientAsync(server.CreateClient());

        // act
        var result = await mcpClient.CallToolAsync("get_with_errors");

        // assert
        result.StructuredContent!
            .RemoveLocations()
            .ToString()
            .ReplaceLineEndings("\n")
            .MatchSnapshot(extension: ".json");
    }

    [Fact]
    public async Task CallTool_GetWithAuthSuccess_ReturnsExpectedResult()
    {
        // arrange
        var storage = new TestMcpStorage();
        await storage.AddOrUpdateToolAsync(
            new OperationToolDefinition(
                Utf8GraphQLParser.Parse("query GetWithAuth { withAuth }")));
        var server = await CreateTestServerAsync(storage);
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
        var storage = new TestMcpStorage();
        await storage.AddOrUpdateToolAsync(
            new OperationToolDefinition(
                Utf8GraphQLParser.Parse("query GetWithAuth { withAuth }")));
        var server = await CreateTestServerAsync(storage);
        var mcpClient = await CreateMcpClientAsync(server.CreateClient());

        // act
        var result = await mcpClient.CallToolAsync("get_with_auth");

        // assert
        result.StructuredContent!
            .RemoveLocations()
            .ToString()
            .ReplaceLineEndings("\n")
            .MatchSnapshot(extension: ".json");
    }

    [Fact]
    public async Task CallTool_WithCustomTool_ReturnsExpectedResult()
    {
        // arrange
        var server =
            await CreateTestServerAsync(
                new TestMcpStorage(),
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
    public async Task ReadResource_Valid_ReturnsResource()
    {
        // arrange
        var storage = new TestMcpStorage();
        var documentNode = Utf8GraphQLParser.Parse(
            await File.ReadAllTextAsync("__resources__/GetBooksWithTitle1.graphql"));
        var tool =
            new OperationToolDefinition(documentNode)
            {
                View = new McpAppView(
                    html: await File.ReadAllTextAsync("__resources__/McpAppView.html"))
                {
                    Csp =
                        new McpAppViewCsp
                        {
                            BaseUriDomains = ["https://example.com"],
                            ConnectDomains = ["https://example.com"],
                            FrameDomains = ["https://example.com"],
                            ResourceDomains = ["https://example.com"]
                        },
                    Domain = "https://example.com",
                    Permissions =
                        new McpAppViewPermissions
                        {
                            Camera = true,
                            ClipboardWrite = false,
                            Geolocation = true,
                            Microphone = false
                        },
                    PrefersBorder = true
                },
                Visibility = [McpAppViewVisibility.App]
            };
        await storage.AddOrUpdateToolAsync(tool);
        var server = await CreateTestServerAsync(storage);
        var mcpClient = await CreateMcpClientAsync(server.CreateClient());

        // act
        var result = await mcpClient.ReadResourceAsync(tool.ViewResourceUri!);

        // assert
        JsonSerializer.Serialize(result, JsonSerializerOptions)
            .ReplaceLineEndings("\n")
            .MatchSnapshot(extension: ".json");
    }

    [Fact]
    public async Task ReadResource_Missing_ThrowsException()
    {
        // arrange
        var storage = new TestMcpStorage();
        var server = await CreateTestServerAsync(storage);
        var mcpClient = await CreateMcpClientAsync(server.CreateClient());

        // act
        async Task Action() => await mcpClient.ReadResourceAsync("ui://views/missing.html");

        // assert
        var exception = await Assert.ThrowsAsync<McpProtocolException>(Action);
        Assert.EndsWith("Resource not found.", exception.Message);
        Assert.Equal(-32002, (int)exception.ErrorCode);
        Assert.Equal("ui://views/missing.html", exception.Data["uri"]);
    }

    [Fact]
    public async Task AddMcp_WithServerOption_SetsOption()
    {
        // arrange & act
        var server =
            await CreateTestServerAsync(
                new TestMcpStorage(),
                configureMcpServerOptions: o => o.InitializationTimeout = TimeSpan.FromSeconds(10));
        await CreateMcpClientAsync(server.CreateClient());
        var executor = await server.Services.GetRequiredService<IRequestExecutorProvider>().GetExecutorAsync();
        var mcpServers = executor.Schema.Services.GetRequiredService<ConcurrentDictionary<string, McpServer>>();
        var options = mcpServers.Values.First().ServerOptions;

        // assert
        Assert.Equal(TimeSpan.FromSeconds(10), options.InitializationTimeout);
    }

    protected abstract Task<TestServer> CreateTestServerAsync(
        IMcpStorage storage,
        ITypeDefinition[]? additionalTypes = null,
        McpDiagnosticEventListener? diagnosticEventListener = null,
        Action<McpServerOptions>? configureMcpServerOptions = null,
        Action<IMcpServerBuilder>? configureMcpServer = null);

    protected static async Task<McpClient> CreateMcpClientAsync(
        HttpClient httpClient,
        string? token = null)
    {
        return
            await McpClient.CreateAsync(
                new HttpClientTransport(
                    new HttpClientTransportOptions
                    {
                        Endpoint = new Uri(httpClient.BaseAddress!, "/graphql/mcp"),
                        AdditionalHeaders = new Dictionary<string, string>
                        {
                            { "Authorization", $"Bearer {token}" }
                        }
                    },
                    httpClient));
    }

    protected static readonly JsonSerializerOptions JsonSerializerOptions =
        new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };

    protected const string TokenIssuer = "test-issuer";
    protected const string TokenAudience = "test-audience";
    protected static readonly SymmetricSecurityKey TokenKey = new("test-secret-key-at-least-32-bytes"u8.ToArray());

    private static readonly string[] s_list = ["test"];

    [McpServerToolType]
    private static class TestTool
    {
        [McpServerTool]
        // ReSharper disable once UnusedMember.Local
        public static string Test(string message) => message;
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
                signingCredentials: new SigningCredentials(TokenKey, SecurityAlgorithms.HmacSha256));

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

public sealed class TestMcpDiagnosticEventListener : McpDiagnosticEventListener
{
    public List<IError> ValidationErrorLog { get; } = [];

    public override void ValidationErrors(IReadOnlyList<IError> errors)
    {
        ValidationErrorLog.AddRange(errors);
    }
}

file static class JsonNodeExtensions
{
    public static JsonNode RemoveLocations(this JsonNode node)
    {
        if (node["errors"] is JsonArray errors)
        {
            foreach (var error in errors)
            {
                if (error is JsonObject errorObject)
                {
                    errorObject.Remove("locations");
                }
            }
        }

        return node;
    }
}
