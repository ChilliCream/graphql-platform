using System.Text;
using System.Text.Json;
using HotChocolate.Adapters.Mcp.Serialization;
using HotChocolate.Adapters.Mcp.Storage;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Adapters.Mcp.Packaging;

public class McpFeatureCollectionArchiveTests : IDisposable
{
    private readonly List<Stream> _streamsToDispose = [];

    [Fact]
    public void Create_WithNullStream_ThrowsArgumentNullException()
    {
        // act & Assert
        Assert.Throws<ArgumentNullException>(() => McpFeatureCollectionArchive.Create(null!));
    }

    [Fact]
    public void Open_WithNullStream_ThrowsArgumentNullException()
    {
        // act & Assert
        Assert.Throws<ArgumentNullException>(() => McpFeatureCollectionArchive.Open(default(Stream)!));
    }

    [Fact]
    public void Open_WithNullString_ThrowsArgumentNullException()
    {
        // act & Assert
        Assert.Throws<ArgumentNullException>(() => McpFeatureCollectionArchive.Open(default(string)!));
    }

    [Fact]
    public async Task SetArchiveMetadata_WithValidData_StoresCorrectly()
    {
        // arrange
        await using var stream = CreateStream();
        var metadata = new ArchiveMetadata
        {
            FormatVersion = new Version("2.0.0")
        };

        // act & Assert
        using var archive = McpFeatureCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);

        // Can read immediately within the same session
        var retrieved = await archive.GetArchiveMetadataAsync();
        Assert.NotNull(retrieved);
        Assert.Equal(metadata.FormatVersion, retrieved.FormatVersion);
    }

    [Fact]
    public async Task GetArchiveMetadata_WhenNotSet_ReturnsNull()
    {
        // arrange
        await using var stream = CreateStream();

        // act & Assert
        using var archive = McpFeatureCollectionArchive.Create(stream);
        var result = await archive.GetArchiveMetadataAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task SetArchiveMetadata_WithNullMetadata_ThrowsArgumentNullException()
    {
        // arrange
        await using var stream = CreateStream();

        // act & Assert
        using var archive = McpFeatureCollectionArchive.Create(stream);
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => archive.SetArchiveMetadataAsync(null!));
    }

    [Fact]
    public async Task CommitAndReopen_PersistsPromptsAndTools()
    {
        // arrange
        await using var stream = CreateStream();
        var metadata = CreateTestMetadata();
        using var promptSettings = JsonDocument.Parse("""{ "title": "Test Prompt" }""");
        var operation = "query GetUsers { users { id name } }"u8.ToArray();
        using var toolSettings = JsonDocument.Parse("""{ "title": "TestTool" }""");
        var viewHtml = "<!-- View HTML -->"u8.ToArray();

        // act - Create and commit
        using (var archive = McpFeatureCollectionArchive.Create(stream, leaveOpen: true))
        {
            await archive.SetArchiveMetadataAsync(metadata);
            await archive.AddPromptAsync("GetUsersPrompt", promptSettings);
            await archive.AddToolAsync("GetUsers", operation, toolSettings, viewHtml);
            await archive.CommitAsync();
        }

        // assert - Reopen and verify persistence
        stream.Position = 0;
        using (var readArchive = McpFeatureCollectionArchive.Open(stream, leaveOpen: true))
        {
            var retrievedMetadata = await readArchive.GetArchiveMetadataAsync();
            Assert.NotNull(retrievedMetadata);
            Assert.Equal(metadata.FormatVersion, retrievedMetadata.FormatVersion);

            var prompt = await readArchive.TryGetPromptAsync("GetUsersPrompt");
            Assert.NotNull(prompt);
            Assert.True(JsonElement.DeepEquals(promptSettings.RootElement, prompt.Settings.RootElement));
            prompt.Dispose();

            using var tool = await readArchive.TryGetToolAsync("GetUsers");
            Assert.NotNull(tool);
            Assert.Equal(operation, tool.Document.ToArray());
            Assert.NotNull(tool.Settings);
            Assert.True(JsonElement.DeepEquals(toolSettings.RootElement, tool.Settings.RootElement));
            Assert.Equal(viewHtml, tool.ViewHtml?.ToArray());
        }
    }

    [Fact]
    public async Task UpdateMode_CanAddNewPromptsToExistingArchive()
    {
        // arrange
        await using var stream = CreateStream();
        var metadata = CreateTestMetadata();
        using var settings1 = JsonDocument.Parse("""{ "messages": [] }""");
        using var settings2 = JsonDocument.Parse("""{ "messages": [] }""");
        const string name1 = "Prompt1";
        const string name2 = "Prompt2";

        // act - Create initial archive
        using (var archive = McpFeatureCollectionArchive.Create(stream, leaveOpen: true))
        {
            await archive.SetArchiveMetadataAsync(metadata);
            await archive.AddPromptAsync(name1, settings1);
            await archive.CommitAsync();
        }

        // act - Update existing archive
        stream.Position = 0;
        using (var updateArchive = McpFeatureCollectionArchive.Open(stream, McpFeatureCollectionArchiveMode.Update, leaveOpen: true))
        {
            await updateArchive.AddPromptAsync(name2, settings2);
            await updateArchive.CommitAsync();
        }

        // assert - Verify both prompts exist
        stream.Position = 0;
        using (var readArchive = McpFeatureCollectionArchive.Open(stream, leaveOpen: true))
        {
            var prompt1 = await readArchive.TryGetPromptAsync(name1);
            Assert.NotNull(prompt1);
            Assert.True(JsonElement.DeepEquals(settings1.RootElement, prompt1.Settings.RootElement));
            prompt1.Dispose();

            var prompt2 = await readArchive.TryGetPromptAsync(name2);
            Assert.NotNull(prompt2);
            Assert.True(JsonElement.DeepEquals(settings2.RootElement, prompt2.Settings.RootElement));
            prompt2.Dispose();
        }
    }

    [Fact]
    public async Task TryGetTool_WithNonExistentTool_ReturnsNull()
    {
        // arrange
        await using var stream = CreateStream();

        // act & Assert
        using var archive = McpFeatureCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(CreateTestMetadata());
        var found = await archive.TryGetToolAsync("non-existent");
        Assert.Null(found);
    }

    [Fact]
    public async Task AddPrompt_WithValidData_StoresCorrectly()
    {
        // arrange
        await using var stream = CreateStream();
        using var settings = JsonDocument.Parse("""{ "title": "Test Prompt" }""");
        var metadata = CreateTestMetadata();

        // act
        using var archive = McpFeatureCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);
        await archive.AddPromptAsync("GetUsersPrompt", settings);

        // assert - Can read immediately within the same session
        var prompt = await archive.TryGetPromptAsync("GetUsersPrompt");

        Assert.NotNull(prompt);
        Assert.Equal("Test Prompt", prompt.Settings.RootElement.GetProperty("title").GetString());

        prompt.Dispose();
    }

    [Fact]
    public async Task AddPrompt_WithMultiplePrompts_StoresAll()
    {
        // arrange
        await using var stream = CreateStream();
        using var settings1 = JsonDocument.Parse("""{ "title": "TestPrompt1" }""");
        using var settings2 = JsonDocument.Parse("""{ "title": "TestPrompt2" }""");
        using var settings3 = JsonDocument.Parse("""{ "title": "TestPrompt3" }""");
        var metadata = CreateTestMetadata();
        const string name1 = "GetUsersPrompt1";
        const string name2 = "GetUsersPrompt2";
        const string name3 = "GetUsersPrompt3";

        // act
        using var archive = McpFeatureCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);
        await archive.AddPromptAsync(name1, settings1);
        await archive.AddPromptAsync(name2, settings2);
        await archive.AddPromptAsync(name3, settings3);

        // assert
        var prompt1 = await archive.TryGetPromptAsync(name1);
        var prompt2 = await archive.TryGetPromptAsync(name2);
        var prompt3 = await archive.TryGetPromptAsync(name3);

        Assert.NotNull(prompt1);
        Assert.Equal("TestPrompt1", prompt1.Settings.RootElement.GetProperty("title").GetString());

        Assert.NotNull(prompt2);
        Assert.Equal("TestPrompt2", prompt2.Settings.RootElement.GetProperty("title").GetString());

        Assert.NotNull(prompt3);
        Assert.Equal("TestPrompt3", prompt3.Settings.RootElement.GetProperty("title").GetString());

        prompt1.Dispose();
        prompt2.Dispose();
        prompt3.Dispose();
    }

    [Fact]
    public async Task AddPrompt_WithoutMetadata_ThrowsInvalidOperationException()
    {
        // arrange
        await using var stream = CreateStream();
        using var settings = JsonDocument.Parse("""{ "title": "Test Prompt" }""");

        // act & Assert
        using var archive = McpFeatureCollectionArchive.Create(stream);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => archive.AddPromptAsync("GetUsersPrompt", settings));
    }

    [Fact]
    public async Task AddPrompt_WithNullSettings_ThrowsArgumentNullException()
    {
        // arrange
        await using var stream = CreateStream();

        // act & Assert
        using var archive = McpFeatureCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(CreateTestMetadata());
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => archive.AddPromptAsync("GetUsersPrompt", null!));
    }

    [Fact]
    public async Task TryGetPrompt_WhenNotExists_ReturnsNull()
    {
        // arrange
        await using var stream = CreateStream();

        // act
        using var archive = McpFeatureCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(CreateTestMetadata());

        var prompt = await archive.TryGetPromptAsync("Nonexistent");

        // assert
        Assert.Null(prompt);
    }

    [Fact]
    public async Task AddPrompt_CommitAndReopen_PersistsPrompts()
    {
        // arrange
        await using var stream = CreateStream();
        using var settings = JsonDocument.Parse("""{ "title": "Test Prompt" }""");
        var metadata = CreateTestMetadata();

        // act - Create and commit
        using (var archive = McpFeatureCollectionArchive.Create(stream, leaveOpen: true))
        {
            await archive.SetArchiveMetadataAsync(metadata);
            await archive.AddPromptAsync("GetUsersPrompt", settings);
            await archive.CommitAsync();
        }

        // assert - Reopen and verify persistence
        stream.Position = 0;
        using (var readArchive = McpFeatureCollectionArchive.Open(stream, leaveOpen: true))
        {
            var prompt = await readArchive.TryGetPromptAsync("GetUsersPrompt");

            Assert.NotNull(prompt);
            Assert.Equal("Test Prompt", prompt.Settings.RootElement.GetProperty("title").GetString());

            prompt.Dispose();
        }
    }

    [Fact]
    public async Task AddPrompt_WithComplexSettings_PreservesJsonStructure()
    {
        // arrange
        await using var stream = CreateStream();
        const string settingsJson = """
            {
                "messages": [
                    {
                        "role": "user",
                        "content": {
                            "type": "text",
                            "text": "Find products related to \"{searchQuery}\" in the product catalog."
                        }
                    }
                ]
            }
            """;
        using var settings = JsonDocument.Parse(settingsJson);
        var metadata = CreateTestMetadata();

        // act
        using var archive = McpFeatureCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);
        await archive.AddPromptAsync("GetUsersPrompt", settings);

        // assert
        var prompt = await archive.TryGetPromptAsync("GetUsersPrompt");

        Assert.NotNull(prompt);
        var retrievedSettings = prompt.Settings.RootElement;
        var messages = retrievedSettings.GetProperty("messages");
        Assert.Equal(1, messages.GetArrayLength());
        Assert.Equal("user", messages[0].GetProperty("role").GetString());
        var content = messages[0].GetProperty("content");
        Assert.Equal("text", content.GetProperty("type").GetString());
        Assert.Equal(
            "Find products related to \"{searchQuery}\" in the product catalog.",
            content.GetProperty("text").GetString());

        prompt.Dispose();
    }

    [Fact]
    public async Task AddTool_WithValidData_StoresCorrectly()
    {
        // arrange
        await using var stream = CreateStream();
        var operation = "query GetUsers { users { id name } }"u8.ToArray();
        var metadata = CreateTestMetadata();

        // act
        using var archive = McpFeatureCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);
        await archive.AddToolAsync("GetUsers", operation, null, null);

        // assert - Can read immediately within the same session
        using var tool = await archive.TryGetToolAsync("GetUsers");

        Assert.NotNull(tool);
        Assert.Equal(operation, tool.Document.ToArray());
    }

    [Fact]
    public async Task AddTool_WithMultipleTools_StoresAll()
    {
        // arrange
        await using var stream = CreateStream();
        var operation1 = "query GetUsers { users { id name } }"u8.ToArray();
        var operation2 = "mutation CreateUser($input: CreateUserInput!) { createUser(input: $input) { id } }"u8.ToArray();
        var operation3 = "mutation DeleteUser($id: ID!) { deleteUser(id: $id) }"u8.ToArray();
        var metadata = CreateTestMetadata();

        // act
        using var archive = McpFeatureCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);
        await archive.AddToolAsync("GetUsers", operation1, null, null);
        await archive.AddToolAsync("CreateUser", operation2, null, null);
        await archive.AddToolAsync("DeleteUser", operation3, null, null);

        // assert
        using var tool1 = await archive.TryGetToolAsync("GetUsers");
        using var tool2 = await archive.TryGetToolAsync("CreateUser");
        using var tool3 = await archive.TryGetToolAsync("DeleteUser");

        Assert.NotNull(tool1);
        Assert.Equal(operation1, tool1.Document.ToArray());

        Assert.NotNull(tool2);
        Assert.Equal(operation2, tool2.Document.ToArray());

        Assert.NotNull(tool3);
        Assert.Equal(operation3, tool3.Document.ToArray());
    }

    [Fact]
    public async Task AddTool_WithoutMetadata_ThrowsInvalidOperationException()
    {
        // arrange
        await using var stream = CreateStream();
        var operation = "query { users { id } }"u8.ToArray();
        using var settings = JsonDocument.Parse("""{ "title": "TestTool" }""");

        // act & Assert
        using var archive = McpFeatureCollectionArchive.Create(stream);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => archive.AddToolAsync("GetUsers", operation, settings, null));
    }

    [Fact]
    public async Task AddTool_WithEmptyDocument_ThrowsArgumentOutOfRangeException()
    {
        // arrange
        await using var stream = CreateStream();
        var operation = ReadOnlyMemory<byte>.Empty;
        using var settings = JsonDocument.Parse("""{ "title": "TestTool" }""");

        // act & Assert
        using var archive = McpFeatureCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(CreateTestMetadata());
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => archive.AddToolAsync("GetUsers", operation, settings, null));
    }

    [Fact]
    public async Task TryGetTool_WhenNotExists_ReturnsNull()
    {
        // arrange
        await using var stream = CreateStream();

        // act
        using var archive = McpFeatureCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(CreateTestMetadata());

        var tool = await archive.TryGetToolAsync("NonExistent");

        // assert
        Assert.Null(tool);
    }

    [Fact]
    public async Task AddTool_CommitAndReopen_PersistsTools()
    {
        // arrange
        await using var stream = CreateStream();
        var operation = "query GetUserById($id: ID!) { userById(id: $id) { id name email } }"u8.ToArray();
        using var settings = JsonDocument.Parse("""{ "title": "TestTool" }""");
        var metadata = CreateTestMetadata();

        // act - Create and commit
        using (var archive = McpFeatureCollectionArchive.Create(stream, leaveOpen: true))
        {
            await archive.SetArchiveMetadataAsync(metadata);
            await archive.AddToolAsync("GetUserById", operation, settings, null);
            await archive.CommitAsync();
        }

        // assert - Reopen and verify persistence
        stream.Position = 0;
        using (var readArchive = McpFeatureCollectionArchive.Open(stream, leaveOpen: true))
        {
            using var tool = await readArchive.TryGetToolAsync("GetUserById");

            Assert.NotNull(tool);
            Assert.Equal(operation, tool.Document.ToArray());
            Assert.Equal("TestTool", tool.Settings?.RootElement.GetProperty("title").GetString());
        }
    }

    [Fact]
    public async Task AddPromptsAndTools_Together_StoresSeparately()
    {
        // arrange
        await using var stream = CreateStream();
        var operation = "query GetUser($id: ID!) { userById(id: $id) { ...UserFields } }"u8.ToArray();
        using var promptSettings = JsonDocument.Parse("""{ "title": "Test Prompt" }""");
        var metadata = CreateTestMetadata();

        // act
        using var archive = McpFeatureCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);
        await archive.AddPromptAsync("GetUserPrompt", promptSettings);
        await archive.AddToolAsync("GetUser", operation, null, null);

        // assert - Prompts
        var prompt = await archive.TryGetPromptAsync("GetUserPrompt");

        Assert.NotNull(prompt);
        Assert.True(JsonElement.DeepEquals(prompt.Settings.RootElement, prompt.Settings.RootElement));

        // assert - Tools
        using var tool = await archive.TryGetToolAsync("GetUser");

        Assert.NotNull(tool);
        Assert.Equal(operation, tool.Document.ToArray());

        prompt.Dispose();
    }

    [Fact]
    public async Task AddPrompt_WithDuplicateKey_ThrowsInvalidOperationException()
    {
        // arrange
        await using var stream = CreateStream();
        using var settings = JsonDocument.Parse("""{ "title": "Test Prompt" }""");
        var metadata = CreateTestMetadata();

        // act & Assert
        using var archive = McpFeatureCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);
        await archive.AddPromptAsync("GetUsersPrompt", settings);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => archive.AddPromptAsync("GetUsersPrompt", settings));
    }

    [Fact]
    public async Task AddTool_WithDuplicateName_ThrowsInvalidOperationException()
    {
        // arrange
        await using var stream = CreateStream();
        var operation = "query GetUsers { users { id } }"u8.ToArray();
        using var settings = JsonDocument.Parse("""{ "title": "Test Prompt" }""");
        var metadata = CreateTestMetadata();

        // act & Assert
        using var archive = McpFeatureCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);
        await archive.AddToolAsync("GetUsers", operation, settings, null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => archive.AddToolAsync("GetUsers", operation, settings, null));
    }

    [Theory]
    [InlineData("GetUsersPrompt")]
    [InlineData("CreateUserPrompt")]
    [InlineData("UpdateUserPrompt")]
    [InlineData("DeleteUserPrompt")]
    public async Task AddPrompt_WithValidNames_Succeeds(string name)
    {
        // arrange
        await using var stream = CreateStream();
        using var settings = JsonDocument.Parse("""{ "title": "Test Prompt" }""");

        // act & Assert - Should not throw
        using var archive = McpFeatureCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(CreateTestMetadata());
        await archive.AddPromptAsync(name, settings);

        var prompt = await archive.TryGetPromptAsync(name);
        Assert.NotNull(prompt);
        prompt.Dispose();
    }

    [Fact]
    public async Task GetArchiveMetadata_ReturnsPromptNamesAndToolNames()
    {
        // arrange
        await using var stream = CreateStream();
        using var promptSettings = JsonDocument.Parse("""{ "title": "Test Prompt" }""");
        var operation = "query GetUsers { users { id } }"u8.ToArray();
        var metadata = CreateTestMetadata();

        // act
        using var archive = McpFeatureCollectionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(metadata);
        await archive.AddPromptAsync("GetUsersPrompt", promptSettings);
        await archive.AddToolAsync("GetUsers", operation, null, null);

        // assert
        var retrievedMetadata = await archive.GetArchiveMetadataAsync();
        Assert.NotNull(retrievedMetadata);
        Assert.Contains("GetUsersPrompt", retrievedMetadata.Prompts);
        Assert.Contains("GetUsers", retrievedMetadata.Tools);
    }

    [Fact]
    public async Task Serialized_Prompt_Should_Be_Parseable_As_Prompt_Definition()
    {
        // arrange
        await using var stream = CreateStream();

        var settingsDto = new McpPromptSettingsDto
        {
            Title = "Search Products",
            Description = "Search for products in the catalog based on a search query.",
            Arguments =
            [
                new McpPromptSettingsArgumentDto
                {
                    Name = "searchQuery",
                    Title = "Search Query",
                    Description = "The search query to find relevant products.",
                    Required = true
                }
            ],
            Icons =
            [
                new McpPromptSettingsIconDto
                {
                    Source = new Uri("https://example.com/icon.png"),
                    MimeType = "image/png",
                    Sizes = ["64x64", "128x128"],
                    Theme = "light"
                }
            ],
            Messages =
            [
                new McpPromptSettingsMessageDto
                {
                    Role = "user",
                    Content = new McpPromptSettingsTextContentDto
                    {
                        Text = "Find products related to \"{searchQuery}\" in the product catalog."
                    }
                }
            ]
        };
        var promptDefinition = PromptDefinition.From("TestPrompt", settingsDto);

        using var settings = McpPromptSettingsSerializer.Format(settingsDto);

        var metadata = CreateTestMetadata();

        // act
        using (var writeArchive = McpFeatureCollectionArchive.Create(stream, leaveOpen: true))
        {
            await writeArchive.SetArchiveMetadataAsync(metadata);
            await writeArchive.AddPromptAsync("TestPrompt", settings);
            await writeArchive.CommitAsync();
        }

        stream.Position = 0;

        PromptDefinition parsedDefinition;
        using (var readArchive = McpFeatureCollectionArchive.Open(stream, leaveOpen: true))
        {
            var readMetadata = await readArchive.GetArchiveMetadataAsync();
            var promptName = readMetadata!.Prompts[0];
            using var prompt = await readArchive.TryGetPromptAsync(promptName);

            var readSettings = McpPromptSettingsSerializer.Parse(prompt!.Settings);

            parsedDefinition = PromptDefinition.From("TestPrompt", readSettings);
        }

        // assert
        Assert.Equal(promptDefinition.Name, parsedDefinition.Name);
        Assert.Equal(promptDefinition.Title, parsedDefinition.Title);
        Assert.Equal(promptDefinition.Description, parsedDefinition.Description);
        Assert.Equal(promptDefinition.Messages.Length, parsedDefinition.Messages.Length);

        for (var i = 0; i < promptDefinition.Arguments?.Length; i++)
        {
            Assert.Equal(promptDefinition.Arguments?[i].Name, parsedDefinition.Arguments?[i].Name);
            Assert.Equal(promptDefinition.Arguments?[i].Title, parsedDefinition.Arguments?[i].Title);
            Assert.Equal(promptDefinition.Arguments?[i].Description, parsedDefinition.Arguments?[i].Description);
            Assert.Equal(promptDefinition.Arguments?[i].Required, parsedDefinition.Arguments?[i].Required);
        }

        for (var i = 0; i < promptDefinition.Icons?.Length; i++)
        {
            Assert.Equal(promptDefinition.Icons?[i].Source, parsedDefinition.Icons?[i].Source);
            Assert.Equal(promptDefinition.Icons?[i].MimeType, parsedDefinition.Icons?[i].MimeType);
            Assert.Equal(promptDefinition.Icons?[i].Sizes, parsedDefinition.Icons?[i].Sizes);
            Assert.Equal(promptDefinition.Icons?[i].Theme, parsedDefinition.Icons?[i].Theme);
        }

        for (var i = 0; i < promptDefinition.Messages.Length; i++)
        {
            Assert.Equal(promptDefinition.Messages[i].Role, parsedDefinition.Messages[i].Role);
            if (promptDefinition.Messages[i].Content is TextContentBlockDefinition originalTextContent
                && parsedDefinition.Messages[i].Content is TextContentBlockDefinition parsedTextContent)
            {
                Assert.Equal(originalTextContent.Text, parsedTextContent.Text);
            }
            else
            {
                Assert.Fail("Message content types do not match.");
            }
        }
    }

    [Fact]
    public async Task Serialized_Tool_Should_Be_Parseable_As_Tool_Definition()
    {
        // arrange
        await using var stream = CreateStream();

        var document = Utf8GraphQLParser.Parse(
            "query GetUserById($id: ID!) { userById(id: $id) { id name email } }"u8.ToArray());
        var settingsDto = new McpToolSettingsDto
        {
            Title = "Get User By ID",
            Icons =
            [
                new McpToolSettingsIconDto
                {
                    Source = new Uri("https://example.com/tool-icon.png"),
                    MimeType = "image/png",
                    Sizes = ["64x64", "128x128"],
                    Theme = "dark"
                }
            ],
            Annotations = new McpToolSettingsAnnotationsDto
            {
                DestructiveHint = false,
                IdempotentHint = true,
                OpenWorldHint = false
            },
            View = new McpToolSettingsMcpAppViewDto
            {
                Csp = new McpToolSettingsCspDto
                {
                    BaseUriDomains = ["https://example.com"],
                    ConnectDomains = ["https://example.com"],
                    FrameDomains = ["https://example.com"],
                    ResourceDomains = ["https://example.com"]
                },
                Domain = "example.com",
                Permissions = new McpToolSettingsPermissionsDto
                {
                    Camera = true,
                    ClipboardWrite = false,
                    Geolocation = true,
                    Microphone = false
                },
                PrefersBorder = true
            },
            Visibility = [McpAppViewVisibility.Model]
        };
        const string viewHtml = "<!-- View HTML -->";
        var toolDefinition = OperationToolDefinition.From(document, "TestTool", settingsDto, viewHtml);

        using var settings = McpToolSettingsSerializer.Format(settingsDto);

        var metadata = CreateTestMetadata();

        // act
        using (var writeArchive = McpFeatureCollectionArchive.Create(stream, leaveOpen: true))
        {
            await writeArchive.SetArchiveMetadataAsync(metadata);

            var ms = new MemoryStream();
            await toolDefinition.Document.PrintToAsync(ms);
            await writeArchive.AddToolAsync(
                toolDefinition.Name,
                ms.ToArray(),
                settings,
                Encoding.UTF8.GetBytes(viewHtml));
            await writeArchive.CommitAsync();
        }

        stream.Position = 0;

        OperationToolDefinition parsedDefinition;
        using (var readArchive = McpFeatureCollectionArchive.Open(stream, leaveOpen: true))
        {
            var readMetadata = await readArchive.GetArchiveMetadataAsync();
            var toolName = readMetadata!.Tools[0];
            using var tool = await readArchive.TryGetToolAsync(toolName);

            var readDocument = Utf8GraphQLParser.Parse(tool!.Document.Span);
            var readSettings = tool.Settings is null ? null : McpToolSettingsSerializer.Parse(tool.Settings);

            var viewHtmlString =
                tool.ViewHtml is { } viewHtmlMemory
                    ? Encoding.UTF8.GetString(viewHtmlMemory.Span)
                    : null;
            parsedDefinition =
                OperationToolDefinition.From(readDocument, toolName, readSettings, viewHtmlString);
        }

        // assert
        Assert.Equal(toolDefinition.Document.ToString(), parsedDefinition.Document.ToString());
        Assert.Equal(toolDefinition.Name, parsedDefinition.Name);
        Assert.Equal(toolDefinition.Title, parsedDefinition.Title);
        Assert.Equal(toolDefinition.Icons?.Length, parsedDefinition.Icons?.Length);

        for (var i = 0; i < toolDefinition.Icons?.Length; i++)
        {
            Assert.Equal(toolDefinition.Icons?[i].Source, parsedDefinition.Icons?[i].Source);
            Assert.Equal(toolDefinition.Icons?[i].MimeType, parsedDefinition.Icons?[i].MimeType);
            Assert.Equal(toolDefinition.Icons?[i].Sizes, parsedDefinition.Icons?[i].Sizes);
            Assert.Equal(toolDefinition.Icons?[i].Theme, parsedDefinition.Icons?[i].Theme);
        }

        Assert.Equal(toolDefinition.DestructiveHint, parsedDefinition.DestructiveHint);
        Assert.Equal(toolDefinition.IdempotentHint, parsedDefinition.IdempotentHint);
        Assert.Equal(toolDefinition.OpenWorldHint, parsedDefinition.OpenWorldHint);
        Assert.Equal(toolDefinition.View?.Html, parsedDefinition.View?.Html);
        Assert.Equal(
            toolDefinition.View?.Csp?.BaseUriDomains,
            parsedDefinition.View?.Csp?.BaseUriDomains);
        Assert.Equal(
            toolDefinition.View?.Csp?.ConnectDomains,
            parsedDefinition.View?.Csp?.ConnectDomains);
        Assert.Equal(
            toolDefinition.View?.Csp?.FrameDomains,
            parsedDefinition.View?.Csp?.FrameDomains);
        Assert.Equal(
            toolDefinition.View?.Csp?.ResourceDomains,
            parsedDefinition.View?.Csp?.ResourceDomains);
        Assert.Equal(toolDefinition.View?.Domain, parsedDefinition.View?.Domain);
        Assert.Equal(
            toolDefinition.View?.Permissions?.Camera,
            parsedDefinition.View?.Permissions?.Camera);
        Assert.Equal(
            toolDefinition.View?.Permissions?.ClipboardWrite,
            parsedDefinition.View?.Permissions?.ClipboardWrite);
        Assert.Equal(
            toolDefinition.View?.Permissions?.Geolocation,
            parsedDefinition.View?.Permissions?.Geolocation);
        Assert.Equal(
            toolDefinition.View?.Permissions?.Microphone,
            parsedDefinition.View?.Permissions?.Microphone);
        Assert.Equal(
            toolDefinition.View?.PrefersBorder,
            parsedDefinition.View?.PrefersBorder);
        Assert.Equal(
            toolDefinition.Visibility,
            parsedDefinition.Visibility);
    }

    private MemoryStream CreateStream()
    {
        var stream = new MemoryStream();
        _streamsToDispose.Add(stream);
        return stream;
    }

    private static ArchiveMetadata CreateTestMetadata()
    {
        return new ArchiveMetadata
        {
            FormatVersion = new Version("1.0.0")
        };
    }

    public void Dispose()
    {
        foreach (var stream in _streamsToDispose)
        {
            stream.Dispose();
        }
    }
}
