namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Workspaces;

public sealed class CurrentWorkspaceCommandTests(NitroCommandFixture fixture) : IClassFixture<NitroCommandFixture>
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "workspace",
                "current",
                "--help")
            .ExecuteAsync();

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
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoSession_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddInteractionMode(mode)
            .AddArguments(
                "workspace",
                "current")
            .ExecuteAsync();

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
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddSession()
            .AddInteractionMode(mode)
            .AddArguments(
                "workspace",
                "current")
            .ExecuteAsync();

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
    public async Task SessionWithWorkspace_ReturnsSuccess(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddSessionWithWorkspace()
            .AddInteractionMode(mode)
            .AddArguments(
                "workspace",
                "current")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            ✓ Currently is Workspace from session selected
            """);
    }

    [Fact]
    public async Task SessionWithWorkspace_ReturnsSuccess_JsonOutput()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
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
