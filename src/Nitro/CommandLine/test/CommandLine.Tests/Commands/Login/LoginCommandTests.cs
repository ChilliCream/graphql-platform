using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Workspaces;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Login;

public sealed class LoginCommandTests(NitroCommandFixture fixture) : IClassFixture<NitroCommandFixture>
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "login",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Log in interactively through your default browser

            Usage:
              nitro login [<url>] [options]

            Arguments:
              <url>  The URL of the Nitro backend (only needed for self-hosted or dedicated deployments)

            Options:
              --cloud-url <cloud-url>  The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: identity.chillicream.com]
              -?, -h, --help           Show help and usage information
            """);
    }

    [Fact]
    public async Task Login_Should_ReturnError_When_NonInteractiveConsole()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments("login")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            'nitro login' requires an interactive console. Use '--api-key' to authenticate
            command invocations in non-interactive environments.
            """);
    }

    [Fact]
    public async Task Login_Should_ReturnError_When_SessionIsNull()
    {
        // arrange
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);

        var builder = new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments("login");

        builder.SessionServiceMock
            .Setup(x => x.LoginAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Session>(null!));

        // act
        var result = await builder.ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was a failure and Nitro could not log you in.
            """);
        Assert.Equal(1, result.ExitCode);

        builder.SessionServiceMock.Verify(
            x => x.LoginAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
        client.VerifyAll();
    }

    [Fact]
    public async Task Login_Should_ReturnError_When_LoginResultHasError()
    {
        // arrange
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);

        var builder = new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments("login");

        builder.SessionServiceMock
            .Setup(x => x.LoginAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ExitException("login_error\nThe login was rejected."));

        // act
        var result = await builder.ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            login_error
            The login was rejected.
            """);
        Assert.Equal(1, result.ExitCode);

        builder.SessionServiceMock.Verify(
            x => x.LoginAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
        client.VerifyAll();
    }

    [Fact]
    public async Task Login_Should_ReturnError_When_NoWorkspacesAvailable()
    {
        // arrange
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        client.Setup(x => x.SelectWorkspacesAsync(
                null,
                5,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new ConnectionPage<ISetDefaultWorkspaceCommand_SelectWorkspace_Query_Me_Workspaces_Edges_Node>(
                    [], null, false));

        var builder = new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments("login");

        builder.SessionServiceMock
            .Setup(x => x.LoginAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSession());

        // act
        var result = await builder.ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            You do not have any workspaces. Run `[bold blue]nitro launch[/]` and create one.
            """);
        Assert.Equal(1, result.ExitCode);

        builder.SessionServiceMock.Verify(
            x => x.LoginAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
        client.VerifyAll();
    }

    [Fact]
    public async Task Login_Should_AutoSelectWorkspace_When_SingleWorkspace()
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
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments("login");

        builder.SessionServiceMock
            .Setup(x => x.LoginAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSession());

        builder.SessionServiceMock
            .Setup(x => x.SelectWorkspaceAsync(
                It.Is<Workspace>(w => w.Id == "ws-1" && w.Name == "my-workspace"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSession(new Workspace("ws-1", "my-workspace")));

        // act
        var result = await builder.ExecuteAsync();

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        builder.SessionServiceMock.Verify(
            x => x.LoginAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
        builder.SessionServiceMock.Verify(
            x => x.SelectWorkspaceAsync(
                It.Is<Workspace>(w => w.Id == "ws-1" && w.Name == "my-workspace"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        client.VerifyAll();
    }

    [Fact]
    public async Task Login_Should_PromptForWorkspace_When_MultipleWorkspaces()
    {
        // arrange
        var client = new Mock<IWorkspacesClient>(MockBehavior.Strict);
        client.Setup(x => x.SelectWorkspacesAsync(
                null,
                5,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateWorkspacePage(
                new SetDefaultWorkspaceCommand_SelectWorkspace_Query_Me_Workspaces_Edges_Node_Workspace(
                    "ws-1", "first-workspace", false),
                new SetDefaultWorkspaceCommand_SelectWorkspace_Query_Me_Workspaces_Edges_Node_Workspace(
                    "ws-2", "second-workspace", false)));

        var builder = new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments("login");

        builder.SessionServiceMock
            .Setup(x => x.LoginAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSession());

        builder.SessionServiceMock
            .Setup(x => x.SelectWorkspaceAsync(
                It.Is<Workspace>(w => w.Id == "ws-1" && w.Name == "first-workspace"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSession(new Workspace("ws-1", "first-workspace")));

        var command = builder.Start();

        // act
        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        builder.SessionServiceMock.Verify(
            x => x.LoginAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
        builder.SessionServiceMock.Verify(
            x => x.SelectWorkspaceAsync(
                It.Is<Workspace>(w => w.Id == "ws-1" && w.Name == "first-workspace"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        client.VerifyAll();
    }

    [Fact]
    public async Task Login_Should_UseUrlFromArgument_When_Provided()
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
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments("login", "https://custom.server.com");

        builder.SessionServiceMock
            .Setup(x => x.LoginAsync(
                "https://custom.server.com",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSession());

        builder.SessionServiceMock
            .Setup(x => x.SelectWorkspaceAsync(
                It.IsAny<Workspace>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSession(new Workspace("ws-1", "my-workspace")));

        // act
        var result = await builder.ExecuteAsync();

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        builder.SessionServiceMock.Verify(
            x => x.LoginAsync("https://custom.server.com", It.IsAny<CancellationToken>()),
            Times.Once);
        client.VerifyAll();
    }

    [Fact]
    public async Task Login_Should_UseUrlFromOption_When_ArgumentNotProvided()
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
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments("login", "--cloud-url", "custom.server.com");

        builder.SessionServiceMock
            .Setup(x => x.LoginAsync(
                "custom.server.com",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSession());

        builder.SessionServiceMock
            .Setup(x => x.SelectWorkspaceAsync(
                It.IsAny<Workspace>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSession(new Workspace("ws-1", "my-workspace")));

        // act
        var result = await builder.ExecuteAsync();

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        builder.SessionServiceMock.Verify(
            x => x.LoginAsync("custom.server.com", It.IsAny<CancellationToken>()),
            Times.Once);
        client.VerifyAll();
    }

    private static Session CreateSession(Workspace? workspace = null)
        => new(
            "session-1",
            "subject-1",
            "tenant-1",
            "https://id.chillicream.com",
            "api.chillicream.com",
            "user@test.com",
            new Tokens(
                "access-token",
                "id-token",
                "refresh-token",
                DateTimeOffset.UtcNow.AddHours(1)),
            workspace);

    private static ConnectionPage<ISetDefaultWorkspaceCommand_SelectWorkspace_Query_Me_Workspaces_Edges_Node>
        CreateWorkspacePage(
            params ISetDefaultWorkspaceCommand_SelectWorkspace_Query_Me_Workspaces_Edges_Node[] items)
        => new(items, null, false);
}
