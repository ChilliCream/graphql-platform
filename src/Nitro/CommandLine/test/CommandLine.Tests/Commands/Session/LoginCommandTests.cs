namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Session;

public sealed class LoginCommandTests(NitroCommandFixture fixture) : SessionCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "login",
            "--help");

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

            Example:
              nitro login
            """);
    }

    [Fact]
    public async Task NonInteractive_ReturnsError()
    {
        // arrange
        SetupInteractionMode(InteractionMode.NonInteractive);

        // act
        var result = await ExecuteCommandAsync("login");

        // assert
        result.AssertError(
            """
            `nitro login` requires an interactive console. Use '--api-key' to authenticate command invocations in non-interactive environments.
            """);
    }

    [Fact]
    public async Task LoginReturnsNull_ReturnsError()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupLoginReturnsNull();

        // act
        var result = await ExecuteCommandAsync("login");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was a failure and Nitro could not log you in.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task LoginThrows_ReturnsError()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupLoginThrows("login_error\nThe login was rejected.");

        // act
        var result = await ExecuteCommandAsync("login");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            login_error
            The login was rejected.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task NoWorkspacesAvailable_ReturnsError()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupLogin();
        SetupSelectWorkspaces();

        // act
        var result = await ExecuteCommandAsync("login");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            You do not have any workspaces. Run `nitro launch` and create one.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task SingleWorkspace_AutoSelects_ReturnsSuccess()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupLogin();
        SetupSelectWorkspaces(CreateWorkspaceNode("ws-1", "my-workspace"));
        SetupSelectWorkspace("ws-1", "my-workspace");

        // act
        var result = await ExecuteCommandAsync("login");

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task MultipleWorkspaces_PromptsForSelection_ReturnsSuccess()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupLogin();
        SetupSelectWorkspaces(
            CreateWorkspaceNode("ws-1", "first-workspace"),
            CreateWorkspaceNode("ws-2", "second-workspace"));
        SetupSelectWorkspace("ws-1", "first-workspace");

        var command = StartInteractiveCommand("login");

        // act
        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task UrlArgument_ReturnsSuccess()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupLogin("https://custom.server.com");
        SetupSelectWorkspaces(CreateWorkspaceNode("ws-1", "my-workspace"));
        SetupSelectWorkspaceAny();

        // act
        var result = await ExecuteCommandAsync("login", "https://custom.server.com");

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task UrlOption_ReturnsSuccess()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupLogin("custom.server.com");
        SetupSelectWorkspaces(CreateWorkspaceNode("ws-1", "my-workspace"));
        SetupSelectWorkspaceAny();

        // act
        var result = await ExecuteCommandAsync("login", "--cloud-url", "custom.server.com");

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
    }
}
