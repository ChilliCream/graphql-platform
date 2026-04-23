namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Apis;

public sealed class ListApiCommandTests(NitroCommandFixture fixture) : ApisCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "api",
            "list",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              List all APIs of a workspace.

            Usage:
              nitro api list [options]

            Options:
                            --cursor <cursor>              The pagination cursor to resume from [env: NITRO_CURSOR]
                            --workspace-id <workspace-id>  The ID of the workspace [env: NITRO_WORKSPACE_ID]
                            --cloud-url <cloud-url>        The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
                            --api-key <api-key>            The API key used for authentication [env: NITRO_API_KEY]
                            --output <json>                The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
                            -?, -h, --help                 Show help and usage information

            Example:
              nitro api list
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoSession_Or_ApiKey_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupNoAuthentication();

        // act
        var result = await ExecuteCommandAsync(
            "api",
            "list");

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run `nitro login`.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoWorkspaceInSession_And_NoWorkspaceOption_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupSession();
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "api",
            "list");

        // assert
        result.AssertError(
            """
            Could not determine workspace. Either login via `nitro login` or specify the '--workspace-id' option.
            """);
    }

    [Fact]
    public async Task ListApisThrows_ReturnsError()
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupListApisQueryException("workspace-from-session");

        // act
        var result = await ExecuteCommandAsync(
            "api",
            "list");

        // assert
        result.AssertError(
            """
            There was an unexpected error: Something unexpected happened.
            """);
    }

    [Fact]
    public async Task WithWorkspaceId_ReturnsSuccess_Interactive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupListApisQuery(
            WorkspaceId,
            apis:
            [
                (ApiId, "products", ["products"], WorkspaceName),
                ("api-2", "catalog", ["catalog"], WorkspaceName)
            ]);

        // act
        var command = StartInteractiveCommand(
            "api",
            "list",
            "--workspace-id",
            WorkspaceId);

        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Fact]
    public async Task WithWorkspaceIdFromSession_ReturnsSuccess_Interactive()
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupInteractionMode(InteractionMode.Interactive);
        SetupListApisQuery(
            "workspace-from-session",
            apis:
            [
                (ApiId, "products", ["products"], WorkspaceName),
                ("api-2", "catalog", ["catalog"], WorkspaceName)
            ]);

        // act
        var command = StartInteractiveCommand(
            "api",
            "list");

        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithWorkspaceIdFromSession_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupInteractionMode(mode);
        SetupListApisQuery(
            "workspace-from-session",
            endCursor: "cursor-2",
            hasNextPage: true,
            apis:
            [
                (ApiId, "products", ["products"], WorkspaceName),
                ("api-2", "catalog", ["catalog"], WorkspaceName)
            ]);

        // act
        var result = await ExecuteCommandAsync(
            "api",
            "list");

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "id": "api-1",
                  "name": "products",
                  "path": "products",
                  "workspace": {
                    "name": "Workspace"
                  },
                  "apiDetailPromptSettings": {
                    "apiDetailPromptSchemaRegistry": {
                      "treatDangerousAsBreaking": true,
                      "allowBreakingSchemaChanges": false
                    }
                  }
                },
                {
                  "id": "api-2",
                  "name": "catalog",
                  "path": "catalog",
                  "workspace": {
                    "name": "Workspace"
                  },
                  "apiDetailPromptSettings": {
                    "apiDetailPromptSchemaRegistry": {
                      "treatDangerousAsBreaking": true,
                      "allowBreakingSchemaChanges": false
                    }
                  }
                }
              ],
              "cursor": "cursor-2"
            }
            """);
    }

    [Fact]
    public async Task WithWorkspaceId_NoData_ReturnsSuccess_Interactive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupListApisQuery(WorkspaceId);

        // act
        var command = StartInteractiveCommand(
            "api",
            "list",
            "--workspace-id",
            WorkspaceId);

        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithWorkspaceId_NoData_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupListApisQuery(WorkspaceId);

        // act
        var result = await ExecuteCommandAsync(
            "api",
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
        SetupSessionWithWorkspace();
        SetupInteractionMode(InteractionMode.Interactive);
        SetupListApisQuery(
            "workspace-from-session",
            cursor: "cursor-1",
            apis:
            [
                (ApiId, "products", ["products"], WorkspaceName)
            ]);

        // act
        var command = StartInteractiveCommand(
            "api",
            "list",
            "--cursor",
            "cursor-1");

        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithCursor_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupInteractionMode(mode);
        SetupListApisQuery(
            "workspace-from-session",
            cursor: "cursor-1",
            apis:
            [
                (ApiId, "products", ["products"], WorkspaceName)
            ]);

        // act
        var result = await ExecuteCommandAsync(
            "api",
            "list",
            "--cursor",
            "cursor-1");

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "id": "api-1",
                  "name": "products",
                  "path": "products",
                  "workspace": {
                    "name": "Workspace"
                  },
                  "apiDetailPromptSettings": {
                    "apiDetailPromptSchemaRegistry": {
                      "treatDangerousAsBreaking": true,
                      "allowBreakingSchemaChanges": false
                    }
                  }
                }
              ],
              "cursor": null
            }
            """);
    }
}
