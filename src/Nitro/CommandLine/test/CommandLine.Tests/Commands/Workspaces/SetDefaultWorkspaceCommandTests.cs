using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Workspaces;
using ChilliCream.Nitro.CommandLine.Commands.Workspaces;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Workspaces;

public sealed class SetDefaultWorkspaceCommandTests(NitroCommandFixture fixture) : IClassFixture<NitroCommandFixture>
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "workspace",
                "set-default",
                "--help")
            .ExecuteAsync();

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
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoSession_Or_ApiKey_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddInteractionMode(mode)
            .AddArguments(
                "workspace",
                "set-default")
            .ExecuteAsync();

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
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        client.Setup(x => x.SelectWorkspacesAsync(
                null,
                5,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConnectionPage<ISetDefaultWorkspaceCommand_SelectWorkspace_Query_Me_Workspaces_Edges_Node>(
                [], null, false));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "workspace",
                "set-default")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            You do not have any workspaces. Run `[bold blue]nitro launch[/]` and create one.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError()
    {
        // arrange
        var client = CreateSelectExceptionClient(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "workspace",
                "set-default")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_NonInteractive()
    {
        // arrange
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        client.Setup(x => x.GetWorkspaceAsync(
                "ws-1",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "workspace",
                "set-default",
                "--workspace-id", "ws-1")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError()
    {
        // arrange
        var client = CreateSelectExceptionClient(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "workspace",
                "set-default")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_NonInteractive()
    {
        // arrange
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        client.Setup(x => x.GetWorkspaceAsync(
                "ws-1",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "workspace",
                "set-default",
                "--workspace-id", "ws-1")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task SelectsWorkspace_ReturnsSuccess_Interactive()
    {
        // arrange
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        client.Setup(x => x.SelectWorkspacesAsync(
                null,
                5,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateWorkspacePage(
                new SetDefaultWorkspaceCommand_SelectWorkspace_Query_Me_Workspaces_Edges_Node_Workspace(
                    "ws-1", "my-workspace", false)));

        var builder = new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "workspace",
                "set-default");

        var command = builder.Start();

        // act
        command.SelectOption(0);

        var result = await command.RunToCompletionAsync();

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
        result.StdOut.MatchInlineSnapshot(
            """
            ? Which workspace do you want to use as your default?

            > my-workspace                                       ? Which workspace do you want to use as your default?: my-workspace
            """);

        client.VerifyAll();
        builder.SessionServiceMock.Verify(
            x => x.SelectWorkspaceAsync(
                It.Is<Workspace>(w => w.Id == "ws-1" && w.Name == "my-workspace"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NonInteractive_NoWorkspaceId_ReturnsError()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "workspace",
                "set-default")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The '--workspace-id' option is required in non-interactive mode.
            """);
    }

    [Fact]
    public async Task NonInteractive_WithWorkspaceId_SetsDefault()
    {
        // arrange
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        client.Setup(x => x.GetWorkspaceAsync(
                "ws-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShowWorkspaceCommandQuery_Node_Workspace("ws-1", "my-workspace", false));

        var builder = new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "workspace",
                "set-default",
                "--workspace-id", "ws-1");

        // act
        var result = await builder.ExecuteAsync();

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
        builder.SessionServiceMock.Verify(
            x => x.SelectWorkspaceAsync(
                It.Is<Workspace>(w => w.Id == "ws-1" && w.Name == "my-workspace"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SetDefault_Should_ReturnError_When_WorkspaceIdNotFound()
    {
        // arrange
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        client.Setup(x => x.GetWorkspaceAsync(
                "ws-999",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IShowWorkspaceCommandQuery_Node?)null);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "workspace",
                "set-default",
                "--workspace-id", "ws-999")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The workspace with ID 'ws-999' was not found.
            """);

        client.VerifyAll();
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
            .ReturnsAsync(CreateWorkspacePage(
                new SetDefaultWorkspaceCommand_SelectWorkspace_Query_Me_Workspaces_Edges_Node_Workspace(
                    "ws-1", "my-workspace", false)));

        var console = Mock.Of<INitroConsole>();
        var sessionService = new Mock<ISessionService>(MockBehavior.Strict);
        sessionService.Setup(x => x.SelectWorkspaceAsync(
                It.Is<Workspace>(w => w.Id == "ws-1" && w.Name == "my-workspace"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Session(
                "session-1",
                "subject-1",
                "tenant-1",
                "https://id.chillicream.com",
                "api.chillicream.com",
                "user@chillicream.com",
                tokens: null,
                workspace: new Workspace("ws-1", "my-workspace")));

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

    private static Mock<IWorkspacesClient> CreateSelectExceptionClient(Exception ex)
    {
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        client.Setup(x => x.SelectWorkspacesAsync(
                null,
                5,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);
        return client;
    }

    private static ConnectionPage<ISetDefaultWorkspaceCommand_SelectWorkspace_Query_Me_Workspaces_Edges_Node>
        CreateWorkspacePage(
            params ISetDefaultWorkspaceCommand_SelectWorkspace_Query_Me_Workspaces_Edges_Node[] items)
    {
        return new ConnectionPage<ISetDefaultWorkspaceCommand_SelectWorkspace_Query_Me_Workspaces_Edges_Node>(
            items, null, false);
    }
}
