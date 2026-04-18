namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Workspaces;

public sealed class ShowWorkspaceCommandTests(NitroCommandFixture fixture)
    : WorkspacesCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "workspace",
            "show",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Show details of a workspace.

            Usage:
              nitro workspace show <id> [options]

            Arguments:
              <id>  The resource ID

            Options:
              --cloud-url <cloud-url>  The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>      The API key used for authentication [env: NITRO_API_KEY]
              --output <json>          The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help           Show help and usage information

            Example:
              nitro workspace show "<workspace-id>"
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
            "workspace",
            "show",
            WorkspaceId);

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
    public async Task WorkspaceNotFound_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupGetWorkspaceQuery(WorkspaceId, null);

        // act
        var result = await ExecuteCommandAsync(
            "workspace",
            "show",
            WorkspaceId);

        // assert
        result.AssertError(
            """
            The workspace with ID 'ws-1' was not found.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithWorkspaceId_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupGetWorkspaceQuery(WorkspaceId,
            CreateShowWorkspaceNode(WorkspaceId, WorkspaceName, false));

        // act
        var result = await ExecuteCommandAsync(
            "workspace",
            "show",
            WorkspaceId);

        // assert
        result.AssertSuccess(
            """
            {
              "id": "ws-1",
              "name": "my-workspace",
              "personal": false
            }
            """);
    }

    [Fact]
    public async Task ShowWorkspaceThrows_ReturnsError()
    {
        // arrange
        SetupGetWorkspaceQueryException(WorkspaceId);

        // act
        var result = await ExecuteCommandAsync(
            "workspace",
            "show",
            WorkspaceId);

        // assert
        result.AssertError(
            """
            There was an unexpected error: Something unexpected happened.
            """);
    }
}
