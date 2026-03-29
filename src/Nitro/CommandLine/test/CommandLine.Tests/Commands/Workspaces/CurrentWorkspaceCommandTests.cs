namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Workspaces;

public sealed class CurrentWorkspaceCommandTests
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddArguments(
                "workspace",
                "current",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Shows the name of the currently selected workspace.

            Usage:
              nitro workspace current [options]

            Options:
              --cloud-url <cloud-url>  The URL of the API. [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>      The API key that is used for the authentication [env: NITRO_API_KEY]
              --output <json>          The format in which the result should be displayed, if this option is set, the console will be non-interactive and the result will be displayed in the specified format [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help           Show help and usage information
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    public async Task NoSession_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddInteractionMode(mode)
            .AddArguments(
                "workspace",
                "current")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            ✕ No workspace selected. Run 'nitro workspace set-default' to set a default.
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task NoSession_ReturnsError_JsonOutput()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "workspace",
                "current")
            .ExecuteAsync();

        // assert
        Assert.Empty(result.StdOut);
        Assert.Empty(result.StdErr);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    public async Task SessionWithoutWorkspace_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddSession()
            .AddInteractionMode(mode)
            .AddArguments(
                "workspace",
                "current")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            ✕ No workspace selected. Run 'nitro workspace set-default' to set a default.
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task SessionWithoutWorkspace_ReturnsError_JsonOutput()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddSession()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "workspace",
                "current")
            .ExecuteAsync();

        // assert
        Assert.Empty(result.StdOut);
        Assert.Empty(result.StdErr);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    public async Task SessionWithWorkspace_ReturnsSuccess(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddSessionWithWorkspace()
            .AddInteractionMode(mode)
            .AddArguments(
                "workspace",
                "current")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            ✓ Currently is Workspace from session selected
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task SessionWithWorkspace_ReturnsSuccess_JsonOutput()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "workspace",
                "current")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            {}
            """);
    }
}
