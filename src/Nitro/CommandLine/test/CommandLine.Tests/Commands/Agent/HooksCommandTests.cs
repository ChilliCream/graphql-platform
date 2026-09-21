namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Tests Claude hook command help and a project-scoped install, status,
/// and uninstall round trip in a temporary workspace.
/// </summary>
public sealed class HooksCommandTests(NitroCommandFixture fixture) : AgentCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_Hooks_ReturnsSuccess()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "--help");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains(
            "Install, inspect, and remove Nitro's turn-boundary hook entries per harness.", result.StdOut);
        Assert.Contains("claude", result.StdOut);
        Assert.Contains("codex", result.StdOut);
    }

    [Fact]
    public async Task Help_HooksClaude_ReturnsSuccess()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "claude", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Install, inspect, and remove Nitro's Claude Code hook entries.

            Usage:
              nitro agent hooks claude [command] [options]

            Options:
              -?, -h, --help  Show help and usage information

            Commands:
              install    Add or update this CLI's Claude Code turn-boundary hook entries.
              status     Show whether this CLI's Claude Code hook entries are missing, current, or outdated.
              uninstall  Remove this CLI's Claude Code turn-boundary hook entries.
            """);
    }

    [Fact]
    public async Task Help_HooksClaudeInstall_ReturnsSuccess()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "claude", "install", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Add or update this CLI's Claude Code turn-boundary hook entries.

            Usage:
              nitro agent hooks claude install [options]

            Options:
              --scope <project|user>  Where the Claude Code settings file lives: 'user' (~/.claude/settings.json) or 'project' (<workspace>/.claude/settings.json) [default: user]
              --output <json>         The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help          Show help and usage information

            Example:
              nitro agent hooks claude install
              nitro agent hooks claude install --scope project
            """);
    }

    [Fact]
    public async Task Help_HooksClaudeStatus_ReturnsSuccess()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "claude", "status", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Show whether this CLI's Claude Code hook entries are missing, current, or outdated.

            Usage:
              nitro agent hooks claude status [options]

            Options:
              --scope <project|user>  Where the Claude Code settings file lives: 'user' (~/.claude/settings.json) or 'project' (<workspace>/.claude/settings.json) [default: user]
              --output <json>         The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help          Show help and usage information

            Example:
              nitro agent hooks claude status
              nitro agent hooks claude status --scope project
            """);
    }

    [Fact]
    public async Task Help_HooksClaudeUninstall_ReturnsSuccess()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "claude", "uninstall", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Remove this CLI's Claude Code turn-boundary hook entries.

            Usage:
              nitro agent hooks claude uninstall [options]

            Options:
              --scope <project|user>  Where the Claude Code settings file lives: 'user' (~/.claude/settings.json) or 'project' (<workspace>/.claude/settings.json) [default: user]
              --output <json>         The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help          Show help and usage information

            Example:
              nitro agent hooks claude uninstall
              nitro agent hooks claude uninstall --scope project
            """);
    }

    [Fact]
    public async Task InstallStatusUninstall_ClaudeGroup_ProjectScope_RoundTripsThroughTheRealCommandPipeline()
    {
        // arrange
        // Redirect the sidecar directory into the temporary test tree.
        var sidecarDirectory = Path.Combine(WorkingDirectory, "..", "app-data");
        SetupGlobalConfigDirectory(sidecarDirectory);
        await InitWorkspaceAsync();

        // act
        var install = await ExecuteCommandAsync("agent", "hooks", "claude", "install", "--scope", "project");

        // assert
        Assert.Equal(0, install.ExitCode);
        Assert.Empty(install.StdErr);
        var settingsPath = Path.Combine(WorkingDirectory, ".claude", "settings.json");
        Assert.True(File.Exists(settingsPath));

        // act
        var statusAfterInstall =
            await ExecuteCommandAsync("agent", "hooks", "claude", "status", "--scope", "project");

        // assert
        Assert.Equal(0, statusAfterInstall.ExitCode);
        Assert.Contains("SessionStart", statusAfterInstall.StdOut);
        Assert.DoesNotContain("missing", statusAfterInstall.StdOut, StringComparison.Ordinal);
        Assert.DoesNotContain("outdated", statusAfterInstall.StdOut, StringComparison.Ordinal);

        // act
        var uninstall = await ExecuteCommandAsync("agent", "hooks", "claude", "uninstall", "--scope", "project");

        // assert
        Assert.Equal(0, uninstall.ExitCode);

        // act
        var statusAfterUninstall =
            await ExecuteCommandAsync("agent", "hooks", "claude", "status", "--scope", "project");

        // assert
        Assert.Equal(1, statusAfterUninstall.ExitCode);
        Assert.DoesNotContain("outdated", statusAfterUninstall.StdOut, StringComparison.Ordinal);
        Assert.DoesNotContain("installed", statusAfterUninstall.StdOut, StringComparison.Ordinal);
    }
}
