namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Environments;

public sealed class ListEnvironmentCommandTests(NitroCommandFixture fixture)
    : EnvironmentsCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "environment",
            "list",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              List all environments of a workspace.

            Usage:
              nitro environment list [options]

            Options:
              --cursor <cursor>              The pagination cursor to resume from [env: NITRO_CURSOR]
              --workspace-id <workspace-id>  The ID of the workspace [env: NITRO_WORKSPACE_ID]
              --cloud-url <cloud-url>        The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>            The API key used for authentication [env: NITRO_API_KEY]
              --output <json>                The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                 Show help and usage information

            Example:
              nitro environment list
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
            "environment",
            "list");

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
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
            "environment",
            "list");

        // assert
        result.AssertError(
            """
            You are not logged in. Run `[bold blue]nitro login[/]` to sign in or manually specify the '--workspace-id' option (if available).
            """);
    }

    [Fact]
    public async Task WithWorkspaceId_ReturnsSuccess_Interactive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupListEnvironmentsQuery(
            WorkspaceId,
            environments:
            [
                (EnvironmentId, EnvironmentName, WorkspaceName),
                ("env-2", "staging", WorkspaceName)
            ]);

        // act
        var command = StartInteractiveCommand(
            "environment",
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
    public async Task WithWorkspaceId_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupListEnvironmentsQuery(
            WorkspaceId,
            environments:
            [
                (EnvironmentId, EnvironmentName, WorkspaceName)
            ]);

        // act
        var result = await ExecuteCommandAsync(
            "environment",
            "list",
            "--workspace-id",
            WorkspaceId);

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "id": "env-1",
                  "name": "production",
                  "workspace": {
                    "name": "workspace-a"
                  }
                }
              ],
              "cursor": null
            }
            """);
    }

    [Fact]
    public async Task WithCursor_ReturnsSuccess_Interactive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupListEnvironmentsQuery(
            WorkspaceId,
            cursor: "cursor-1",
            environments:
            [
                ("env-2", "staging", WorkspaceName)
            ]);

        // act
        var command = StartInteractiveCommand(
            "environment",
            "list",
            "--workspace-id",
            WorkspaceId,
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
        SetupInteractionMode(mode);
        SetupListEnvironmentsQuery(
            WorkspaceId,
            cursor: "cursor-1",
            environments:
            [
                ("env-2", "staging", WorkspaceName)
            ]);

        // act
        var result = await ExecuteCommandAsync(
            "environment",
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
                  "id": "env-2",
                  "name": "staging",
                  "workspace": {
                    "name": "workspace-a"
                  }
                }
              ],
              "cursor": null
            }
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoEnvironments_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupListEnvironmentsQuery(WorkspaceId);

        // act
        var result = await ExecuteCommandAsync(
            "environment",
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
    public async Task ListEnvironmentsThrows_ReturnsError()
    {
        // arrange
        SetupListEnvironmentsQueryException(WorkspaceId);

        // act
        var result = await ExecuteCommandAsync(
            "environment",
            "list",
            "--workspace-id",
            WorkspaceId);

        // assert
        result.AssertError(
            """
            There was an unexpected error: Something unexpected happened.
            """);
    }
}
