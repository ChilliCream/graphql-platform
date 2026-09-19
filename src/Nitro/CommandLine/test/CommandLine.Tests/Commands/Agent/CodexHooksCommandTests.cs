namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Covers command wiring (help text) for <c>agent hooks codex install/status/uninstall</c>; nothing
/// here installs against the real <c>~/.codex</c>. The install/status/uninstall and config.toml
/// wrap/restore behavior is covered by the dedicated <c>CodexHooksEditor</c>,
/// <c>CodexConfigTomlNotifyEditor</c>, and <c>CodexHooksInstallerService</c> test classes.
/// </summary>
public sealed class CodexHooksCommandTests(NitroCommandFixture fixture) : AgentCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_Hooks_ListsEverySupportedHarness()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "--help");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("claude", result.StdOut);
        Assert.Contains("codex", result.StdOut);
    }

    [Fact]
    public async Task Help_HooksCodex_ReturnsSuccess()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "codex", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Install, inspect, and remove Nitro's Codex CLI hook and notify entries.

            Usage:
              nitro agent hooks codex [command] [options]

            Options:
              -?, -h, --help  Show help and usage information

            Commands:
              install    Add or update this CLI's Codex CLI turn-boundary hook and notify entries.
              status     Show whether this CLI's Codex CLI hook and notify entries are missing, current, or outdated.
              uninstall  Remove this CLI's Codex CLI turn-boundary hook entries and restore any wrapped foreign notify program.
            """);
    }

    [Fact]
    public async Task Help_HooksCodexInstall_ReturnsSuccess()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "codex", "install", "--help");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Add or update this CLI's Codex CLI turn-boundary hook and notify entries.", result.StdOut);
    }

    [Fact]
    public async Task Help_HooksCodexStatus_ReturnsSuccess()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "codex", "status", "--help");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains(
            "Show whether this CLI's Codex CLI hook and notify entries are missing, current, or outdated.",
            result.StdOut);
    }

    [Fact]
    public async Task Help_HooksCodexUninstall_ReturnsSuccess()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "codex", "uninstall", "--help");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains(
            "Remove this CLI's Codex CLI turn-boundary hook entries and restore any wrapped foreign notify program.",
            result.StdOut);
    }
}
