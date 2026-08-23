namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Command wiring (help text) only for <c>agent hooks copilot
/// install/status/uninstall</c>, same reasoning as
/// <c>CodexHooksCommandTests</c>: this test harness has no plumbing to
/// redirect <c>COPILOT_HOME</c> away from the real <c>~/.copilot</c>, so
/// nothing here ever actually installs. The deep install/status/uninstall
/// behavior is exercised directly against
/// <c>CopilotHooksEditor</c>/<c>CopilotHooksInstallerService</c> in their own
/// dedicated test classes, all fixture- and temp-directory-driven.
/// </summary>
public sealed class CopilotHooksCommandTests(NitroCommandFixture fixture) : AgentCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_Hooks_ListsCopilotAlongsideTheExistingClaudeAndCodexVerbs()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "--help");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("install", result.StdOut);
        Assert.Contains("status", result.StdOut);
        Assert.Contains("uninstall", result.StdOut);
        Assert.Contains("codex", result.StdOut);
        Assert.Contains("copilot", result.StdOut);
    }

    [Fact]
    public async Task Help_HooksCopilot_ReturnsSuccess()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "copilot", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Install, inspect, and remove Nitro's Copilot CLI hook entries.

            Usage:
              nitro agent hooks copilot [command] [options]

            Options:
              -?, -h, --help  Show help and usage information

            Commands:
              install    Add or update this CLI's Copilot CLI turn-boundary hook entries.
              status     Show whether this CLI's Copilot CLI hook entries are missing, current, or outdated.
              uninstall  Remove this CLI's Copilot CLI turn-boundary hook entries.
            """);
    }

    [Fact]
    public async Task Help_HooksCopilotInstall_ReturnsSuccess()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "copilot", "install", "--help");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Add or update this CLI's Copilot CLI turn-boundary hook entries.", result.StdOut);
    }

    [Fact]
    public async Task Help_HooksCopilotStatus_ReturnsSuccess()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "copilot", "status", "--help");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains(
            "Show whether this CLI's Copilot CLI hook entries are missing, current, or outdated.", result.StdOut);
    }

    [Fact]
    public async Task Help_HooksCopilotUninstall_ReturnsSuccess()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "copilot", "uninstall", "--help");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Remove this CLI's Copilot CLI turn-boundary hook entries.", result.StdOut);
    }
}
