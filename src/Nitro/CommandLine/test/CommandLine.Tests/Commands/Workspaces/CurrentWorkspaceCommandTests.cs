using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Workspaces;

public sealed class CurrentWorkspaceCommandTests
{
    [Fact]
    public async Task Current_WithSelectedWorkspace_ReturnsSuccessAndMessage()
    {
        // arrange
        var host = CreateHost(TestSessionService.WithWorkspace());

        // act
        var exitCode = await host.InvokeAsync("workspace", "current");

        // assert
        Assert.Equal(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            ✓ Currently is Workspace selected
            """);
        Assert.Empty(host.StdErr);
    }

    [Fact]
    public async Task Current_WithoutSelectedWorkspace_ReturnsErrorAndMessage()
    {
        // arrange
        var host = CreateHost(new TestSessionService());

        // act
        var exitCode = await host.InvokeAsync("workspace", "current");

        // assert
        Assert.NotEqual(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            ✕ No workspace selected. Run 'nitro workspace set-default' to set a default.
            """);
        Assert.Empty(host.StdErr);
    }

    private static CommandBuilder CreateHost(TestSessionService session)
    {
        var host = new CommandBuilder()
            .AddService<ISessionService>(session);

        return host;
    }
}
