using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Services.ProjectSettings;

namespace ChilliCream.Nitro.CommandLine.Tests.Services.ProjectSettings;

public sealed class ProjectSettingsTests
{
    [Fact]
    public void Deserialize_BasicJson_WithWorkspaceIdAndStage()
    {
        // arrange
        const string json = """
            {
                "workspaceId": "ws-123",
                "defaultStage": "dev"
            }
            """;

        // act
        var settings = JsonSerializer.Deserialize(
            json, ProjectSettingsJsonContext.Default.ProjectSettings);

        // assert
        Assert.NotNull(settings);
        Assert.Equal("ws-123", settings.WorkspaceId);
        Assert.Equal("dev", settings.DefaultStage);
    }

    [Fact]
    public void Deserialize_WithApisArray()
    {
        // arrange
        const string json = """
            {
                "workspaceId": "ws-456",
                "apis": [
                    {
                        "id": "api-1",
                        "name": "My API",
                        "path": "src/api",
                        "defaultStage": "staging"
                    },
                    {
                        "id": "api-2",
                        "path": "src/api2"
                    }
                ]
            }
            """;

        // act
        var settings = JsonSerializer.Deserialize(
            json, ProjectSettingsJsonContext.Default.ProjectSettings);

        // assert
        Assert.NotNull(settings);
        Assert.NotNull(settings.Apis);
        Assert.Equal(2, settings.Apis.Count);
        Assert.Equal("api-1", settings.Apis[0].Id);
        Assert.Equal("My API", settings.Apis[0].Name);
        Assert.Equal("src/api", settings.Apis[0].Path);
        Assert.Equal("staging", settings.Apis[0].DefaultStage);
        Assert.Equal("api-2", settings.Apis[1].Id);
        Assert.Null(settings.Apis[1].Name);
    }

    [Fact]
    public void Deserialize_WithStyleTags()
    {
        // arrange
        const string json = """
            {
                "styleTags": ["graphql", "relay", "custom"]
            }
            """;

        // act
        var settings = JsonSerializer.Deserialize(
            json, ProjectSettingsJsonContext.Default.ProjectSettings);

        // assert
        Assert.NotNull(settings);
        Assert.NotNull(settings.StyleTags);
        Assert.Equal(3, settings.StyleTags.Count);
        Assert.Equal("graphql", settings.StyleTags[0]);
        Assert.Equal("relay", settings.StyleTags[1]);
        Assert.Equal("custom", settings.StyleTags[2]);
    }

    [Fact]
    public void Deserialize_EmptyJsonObject()
    {
        // arrange
        const string json = "{}";

        // act
        var settings = JsonSerializer.Deserialize(
            json, ProjectSettingsJsonContext.Default.ProjectSettings);

        // assert
        Assert.NotNull(settings);
        Assert.Null(settings.WorkspaceId);
        Assert.Null(settings.DefaultStage);
        Assert.Null(settings.CloudUrl);
        Assert.Null(settings.Version);
        Assert.Null(settings.Apis);
        Assert.Null(settings.Clients);
        Assert.Null(settings.StyleTags);
        Assert.Null(settings.McpCollections);
        Assert.Null(settings.OpenApiCollections);
    }

    [Fact]
    public void Deserialize_WithAllFields()
    {
        // arrange
        const string json = """
            {
                "version": "1.0",
                "workspaceId": "ws-full",
                "cloudUrl": "https://cloud.example.com",
                "defaultStage": "production",
                "styleTags": ["tag1"],
                "apis": [{"id": "a1"}],
                "clients": [{"id": "c1", "name": "Client One", "path": "clients/c1"}],
                "mcpCollections": [{"id": "mcp1", "promptPatterns": ["*.md"], "toolPatterns": ["*.tool"]}],
                "openApiCollections": [{"id": "oa1", "filePatterns": ["*.yaml"]}]
            }
            """;

        // act
        var settings = JsonSerializer.Deserialize(
            json, ProjectSettingsJsonContext.Default.ProjectSettings);

        // assert
        Assert.NotNull(settings);
        Assert.Equal("1.0", settings.Version);
        Assert.Equal("ws-full", settings.WorkspaceId);
        Assert.Equal("https://cloud.example.com", settings.CloudUrl);
        Assert.Equal("production", settings.DefaultStage);
        Assert.Single(settings.StyleTags!);
        Assert.Single(settings.Apis!);
        Assert.Single(settings.Clients!);
        Assert.Equal("Client One", settings.Clients![0].Name);
        Assert.Single(settings.McpCollections!);
        Assert.Equal("*.md", settings.McpCollections![0].PromptPatterns![0]);
        Assert.Single(settings.OpenApiCollections!);
        Assert.Equal("*.yaml", settings.OpenApiCollections![0].FilePatterns![0]);
    }

    [Fact]
    public void Deserialize_NullOptionalFields_AreIgnored()
    {
        // arrange
        const string json = """
            {
                "workspaceId": null,
                "apis": null,
                "styleTags": null
            }
            """;

        // act
        var settings = JsonSerializer.Deserialize(
            json, ProjectSettingsJsonContext.Default.ProjectSettings);

        // assert
        Assert.NotNull(settings);
        Assert.Null(settings.WorkspaceId);
        Assert.Null(settings.Apis);
        Assert.Null(settings.StyleTags);
    }

    [Fact]
    public void Serialize_RoundTrips_Correctly()
    {
        // arrange
        var settings = new CommandLine.Services.ProjectSettings.ProjectSettings
        {
            WorkspaceId = "ws-rt",
            DefaultStage = "dev",
            Apis =
            [
                new ApiSettings { Id = "a1", Path = "src/a1" }
            ]
        };

        // act
        var json = JsonSerializer.Serialize(
            settings, ProjectSettingsJsonContext.Default.ProjectSettings);
        var deserialized = JsonSerializer.Deserialize(
            json, ProjectSettingsJsonContext.Default.ProjectSettings);

        // assert
        Assert.NotNull(deserialized);
        Assert.Equal("ws-rt", deserialized.WorkspaceId);
        Assert.Equal("dev", deserialized.DefaultStage);
        Assert.Single(deserialized.Apis!);
        Assert.Equal("a1", deserialized.Apis![0].Id);
    }
}
