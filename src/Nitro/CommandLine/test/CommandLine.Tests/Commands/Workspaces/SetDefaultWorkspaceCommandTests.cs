using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Workspaces;
using ChilliCream.Nitro.CommandLine.Commands.Workspaces;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Workspaces;

public sealed class SetDefaultWorkspaceCommandTests(NitroCommandFixture fixture)
    : WorkspacesCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "workspace",
            "set-default",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Set the default workspace.

            Usage:
              nitro workspace set-default [options]

            Options:
              --workspace-id <workspace-id>  The ID of the workspace [env: NITRO_WORKSPACE_ID]
              --cloud-url <cloud-url>        The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>            The API key used for authentication [env: NITRO_API_KEY]
              --output <json>                The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                 Show help and usage information

            Example:
              nitro workspace set-default
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
            "set-default");

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    [Fact]
    public async Task NoWorkspaces_ReturnsError()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupSelectWorkspacesQuery();

        // act
        var result = await ExecuteCommandAsync(
            "workspace",
            "set-default");

        // assert
        result.AssertError(
            """
            You do not have any workspaces. Run `[bold blue]nitro launch[/]` and create one.
            """);
    }

    [Fact]
    public async Task SetDefaultWorkspaceThrows_ReturnsError()
    {
        // arrange
        SetupGetWorkspaceQueryException(WorkspaceId);

        // act
        var result = await ExecuteCommandAsync(
            "workspace",
            "set-default",
            "--workspace-id", WorkspaceId);

        // assert
        result.AssertError(
            """
            There was an unexpected error: Something unexpected happened.
            """);
    }

    [Fact]
    public async Task SelectsWorkspace_ReturnsSuccess_Interactive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupSelectWorkspacesQuery(
            new SetDefaultWorkspaceCommand_SelectWorkspace_Query_Me_Workspaces_Edges_Node_Workspace(
                WorkspaceId, WorkspaceName, false));

        var command = StartInteractiveCommand(
            "workspace",
            "set-default");

        // act
        command.SelectOption(0);

        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Fact]
    public async Task NoWorkspaceId_ReturnsError()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "workspace",
            "set-default");

        // assert
        result.AssertError(
            """
            The '--workspace-id' option is required in non-interactive mode.
            """);
    }

    [Fact]
    public async Task WithWorkspaceId_ReturnsSuccess()
    {
        // arrange
        SetupGetWorkspaceQuery(WorkspaceId,
            CreateShowWorkspaceNode(WorkspaceId, WorkspaceName, false));

        // act
        var result = await ExecuteCommandAsync(
            "workspace",
            "set-default",
            "--workspace-id", WorkspaceId);

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        SessionServiceMock.Verify(
            x => x.SelectWorkspaceAsync(
                It.Is<Services.Sessions.Workspace>(w => w.Id == WorkspaceId && w.Name == WorkspaceName),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task WithWorkspaceId_WorkspaceNotFound_ReturnsError()
    {
        // arrange
        SetupGetWorkspaceQuery("ws-999", null);

        // act
        var result = await ExecuteCommandAsync(
            "workspace",
            "set-default",
            "--workspace-id", "ws-999");

        // assert
        result.AssertError(
            """
            The workspace with ID 'ws-999' was not found.
            """);
    }

    [Fact]
    public async Task SetDefault_Should_AutoSelect_When_SingleWorkspaceAndNotForced()
    {
        // arrange
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        client.Setup(x => x.SelectWorkspacesAsync(
                null,
                5,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConnectionPage<ISetDefaultWorkspaceCommand_SelectWorkspace_Query_Me_Workspaces_Edges_Node>(
                [
                    new SetDefaultWorkspaceCommand_SelectWorkspace_Query_Me_Workspaces_Edges_Node_Workspace(
                        "ws-1", "my-workspace", false)
                ], null, false));

        var console = Mock.Of<INitroConsole>();
        var sessionService = new Mock<Services.Sessions.ISessionService>(MockBehavior.Strict);
        sessionService.Setup(x => x.SelectWorkspaceAsync(
                It.Is<Services.Sessions.Workspace>(w => w.Id == "ws-1" && w.Name == "my-workspace"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Services.Sessions.Session(
                "session-1",
                "subject-1",
                "tenant-1",
                "https://id.chillicream.com",
                "api.chillicream.com",
                "user@chillicream.com",
                tokens: null,
                workspace: new Services.Sessions.Workspace("ws-1", "my-workspace")));

        // act
        var exitCode = await SetDefaultWorkspaceCommand.ExecuteAsync(
            forceSelection: false,
            console,
            client.Object,
            sessionService.Object,
            CancellationToken.None);

        // assert
        Assert.Equal(0, exitCode);

        client.VerifyAll();
        sessionService.VerifyAll();
    }
}
