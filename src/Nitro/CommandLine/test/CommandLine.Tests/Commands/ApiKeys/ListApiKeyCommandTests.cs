namespace ChilliCream.Nitro.CommandLine.Tests.Commands.ApiKeys;

public sealed class ListApiKeyCommandTests(NitroCommandFixture fixture) : ApiKeysCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "api-key",
            "list",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              List all API keys of a workspace.

            Usage:
              nitro api-key list [options]

            Options:
                            --cursor <cursor>              The pagination cursor to resume from [env: NITRO_CURSOR]
                            --workspace-id <workspace-id>  The ID of the workspace [env: NITRO_WORKSPACE_ID]
                            --cloud-url <cloud-url>        The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
                            --api-key <api-key>            The API key used for authentication [env: NITRO_API_KEY]
                            --output <json>                The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
                            -?, -h, --help                 Show help and usage information

            Example:
              nitro api-key list
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoSession_And_NoWorkspaceId_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        SetupInteractionMode(mode);
        SetupNoAuthentication();

        var result = await ExecuteCommandAsync(
            "api-key",
            "list");

        // assert
        result.AssertError(
            """
            You are not logged in. Run `[bold blue]nitro login[/]` to sign in or manually specify the '--workspace-id' option (if available).
            """);
    }

    [Fact]
    public async Task WithWorkspaceIdFromSession_ReturnsSuccess_Interactive()
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupInteractionMode(InteractionMode.Interactive);
        SetupListApiKeysQuery(
            "workspace-from-session",
            apiKeys: [("key-1", "tenant-key", "Workspace"), ("key-2", "integration-key", "Workspace")]);

        var command = StartInteractiveCommand(
            "api-key",
            "list");

        // act
        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithWorkspaceId_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupListApiKeysQuery(
            WorkspaceId,
            apiKeys: [("key-1", "tenant-key", "Workspace"), ("key-2", "integration-key", "Workspace")]);

        // act
        var result = await ExecuteCommandAsync(
            "api-key",
            "list",
            "--workspace-id",
            WorkspaceId);

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "id": "key-1",
                  "name": "tenant-key",
                  "workspace": {
                    "name": "Workspace"
                  }
                },
                {
                  "id": "key-2",
                  "name": "integration-key",
                  "workspace": {
                    "name": "Workspace"
                  }
                }
              ],
              "cursor": null
            }
            """);
    }

    [Fact]
    public async Task WithWorkspaceId_NoData_ReturnsSuccess_Interactive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupListApiKeysQuery(WorkspaceId);

        var command = StartInteractiveCommand(
            "api-key",
            "list",
            "--workspace-id",
            WorkspaceId);

        // act
        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithWorkspaceIdFromSession_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupInteractionMode(mode);
        SetupListApiKeysQuery(
            "workspace-from-session",
            endCursor: "cursor-2",
            hasNextPage: true,
            apiKeys: [("key-1", "tenant-key", "Workspace"), ("key-2", "integration-key", "Workspace")]);

        // act
        var result = await ExecuteCommandAsync(
            "api-key",
            "list");

        // assert
        result.AssertSuccess(
            """
            {
                "values": [
                    {
                        "id": "key-1",
                        "name": "tenant-key",
                        "workspace": {
                            "name": "Workspace"
                        }
                    },
                    {
                        "id": "key-2",
                        "name": "integration-key",
                        "workspace": {
                            "name": "Workspace"
                        }
                    }
                ],
                "cursor": "cursor-2"
            }
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithWorkspaceId_NoData_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupListApiKeysQuery(WorkspaceId);

        // act
        var result = await ExecuteCommandAsync(
            "api-key",
            "list",
            "--workspace-id",
            WorkspaceId);

        // assert
        result.AssertSuccess(
            """
            {
              "values": [],
              "cursor": null
            }
            """);
    }

    [Fact]
    public async Task WithCursor_ReturnsSuccess_Interactive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupListApiKeysQuery(
            WorkspaceId,
            cursor: "cursor-1",
            apiKeys: [("key-1", "tenant-key", "Workspace"), ("key-2", "integration-key", "Workspace")]);

        var command = StartInteractiveCommand(
            "api-key",
            "list",
            "--workspace-id",
            WorkspaceId,
            "--cursor",
            "cursor-1");

        // act
        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithCursor_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupListApiKeysQuery(
            WorkspaceId,
            cursor: "cursor-1",
            apiKeys: [("key-1", "tenant-key", "Workspace"), ("key-2", "integration-key", "Workspace")]);

        // act
        var result = await ExecuteCommandAsync(
            "api-key",
            "list",
            "--workspace-id",
            WorkspaceId,
            "--cursor",
            "cursor-1");

        // assert
        result.AssertSuccess(
            """
            {
                "values": [
                    {
                        "id": "key-1",
                        "name": "tenant-key",
                        "workspace": {
                            "name": "Workspace"
                        }
                    },
                    {
                        "id": "key-2",
                        "name": "integration-key",
                        "workspace": {
                            "name": "Workspace"
                        }
                    }
                ],
                "cursor": null
            }
            """);
    }

    [Fact]
    public async Task ListApiKeysThrows_ReturnsError()
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupListApiKeysQueryException("workspace-from-session");

        // act
        var result = await ExecuteCommandAsync(
            "api-key",
            "list");

        // assert
        result.AssertError(
            """
            There was an unexpected error: Something unexpected happened.
            """);
    }
}
