namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Workspaces;

public sealed class CurrentWorkspaceCommandTests(NitroCommandFixture fixture)
    : WorkspacesCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "workspace",
            "current",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Show the name of the currently selected workspace.

            Usage:
              nitro workspace current [options]

            Options:
              --cloud-url <cloud-url>  The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>      The API key used for authentication [env: NITRO_API_KEY]
              --output <json>          The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help           Show help and usage information

            Example:
              nitro workspace current
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoSession_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupNoAuthentication();

        // act
        var result = await ExecuteCommandAsync(
            "workspace",
            "current");

        // assert
        Assert.Empty(result.StdOut);
        result.StdErr.MatchInlineSnapshot(
            """
            No workspace selected. Run 'nitro workspace set-default' to set a default.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task SessionWithoutWorkspace_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSession();

        // act
        var result = await ExecuteCommandAsync(
            "workspace",
            "current");

        // assert
        Assert.Empty(result.StdOut);
        result.StdErr.MatchInlineSnapshot(
            """
            No workspace selected. Run 'nitro workspace set-default' to set a default.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task SessionWithWorkspace_ReturnsSuccess()
    {
        // arrange
        SetupSessionWithWorkspace();

        // act
        var result = await ExecuteCommandAsync(
            "workspace",
            "current");

        // assert
        result.AssertSuccess(
            """
            The current workspace is: [bold blue]Workspace from session[/]
            """);
    }
}
