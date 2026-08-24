namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Command wiring (help text) for <c>agent hooks claude install/status/uninstall</c>
/// and their deprecated bare-verb aliases, plus one full functional round
/// trip run with <c>--scope project</c> through each surface: the deep
/// install/status/uninstall behavior (golden fixtures: missing,
/// foreign-only, mixed, already-installed, outdated, manually-edited,
/// concurrently-edited) is exercised directly against
/// <c>ClaudeHooksEditor</c>/<c>ClaudeHooksInstallerService</c> in
/// <c>ClaudeHooksEditorTests</c>/<c>ClaudeHooksInstallerServiceTests</c>.
/// <c>--scope user</c> is never exercised at this layer: its path resolves
/// through the REAL OS home directory
/// (<see cref="Environment.SpecialFolder.UserProfile"/>), which this test
/// process must never write to - only <c>--scope project</c>, which resolves
/// under this test's own temp workspace, is safe to run for real here. The
/// sidecar's global config directory is also redirected into the temp
/// workspace, so no run in this class ever touches the real platform
/// application-data directory either.
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
        Assert.Contains("install", result.StdOut);
        Assert.Contains("status", result.StdOut);
        Assert.Contains("uninstall", result.StdOut);
        Assert.Contains("claude", result.StdOut);
        Assert.Contains("codex", result.StdOut);
        Assert.Contains("copilot", result.StdOut);
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
    public async Task Help_HooksInstall_ReturnsSuccess()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "install", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Add or update this CLI's Claude Code turn-boundary hook entries.

            Usage:
              nitro agent hooks install [options]

            Options:
              --scope <project|user>  Where the Claude Code settings file lives: 'user' (~/.claude/settings.json) or 'project' (<workspace>/.claude/settings.json) [default: user]
              --output <json>         The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help          Show help and usage information

            Example:
              nitro agent hooks install
              nitro agent hooks install --scope project
            """);
    }

    [Fact]
    public async Task Help_HooksStatus_ReturnsSuccess()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "status", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Show whether this CLI's Claude Code hook entries are missing, current, or outdated.

            Usage:
              nitro agent hooks status [options]

            Options:
              --scope <project|user>  Where the Claude Code settings file lives: 'user' (~/.claude/settings.json) or 'project' (<workspace>/.claude/settings.json) [default: user]
              --output <json>         The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help          Show help and usage information

            Example:
              nitro agent hooks status
              nitro agent hooks status --scope project
            """);
    }

    [Fact]
    public async Task Help_HooksUninstall_ReturnsSuccess()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "uninstall", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Remove this CLI's Claude Code turn-boundary hook entries.

            Usage:
              nitro agent hooks uninstall [options]

            Options:
              --scope <project|user>  Where the Claude Code settings file lives: 'user' (~/.claude/settings.json) or 'project' (<workspace>/.claude/settings.json) [default: user]
              --output <json>         The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help          Show help and usage information

            Example:
              nitro agent hooks uninstall
              nitro agent hooks uninstall --scope project
            """);
    }

    [Fact]
    public async Task InstallStatusUninstall_ClaudeGroup_ProjectScope_RoundTripsThroughTheRealCommandPipeline()
    {
        // arrange: redirect the sidecar's global config directory into this
        // test's own temp tree - the only override this scope needs, since
        // --scope project already resolves its settings path under
        // AgentCommandTestBase's own TestFileSystem-rooted WorkingDirectory.
        var sidecarDirectory = Path.Combine(WorkingDirectory, "..", "app-data");
        SetupGlobalConfigDirectory(sidecarDirectory);
        await InitWorkspaceAsync();

        // act: install
        var install = await ExecuteCommandAsync("agent", "hooks", "claude", "install", "--scope", "project");

        // assert: install
        Assert.Equal(0, install.ExitCode);
        Assert.Empty(install.StdErr);
        var settingsPath = Path.Combine(WorkingDirectory, ".claude", "settings.json");
        Assert.True(File.Exists(settingsPath));

        // act: status after install
        var statusAfterInstall =
            await ExecuteCommandAsync("agent", "hooks", "claude", "status", "--scope", "project");

        // assert: status after install
        Assert.Equal(0, statusAfterInstall.ExitCode);
        Assert.Contains("SessionStart", statusAfterInstall.StdOut);
        Assert.DoesNotContain("missing", statusAfterInstall.StdOut, StringComparison.Ordinal);
        Assert.DoesNotContain("outdated", statusAfterInstall.StdOut, StringComparison.Ordinal);

        // act: uninstall
        var uninstall = await ExecuteCommandAsync("agent", "hooks", "claude", "uninstall", "--scope", "project");

        // assert: uninstall
        Assert.Equal(0, uninstall.ExitCode);

        // act: status after uninstall
        var statusAfterUninstall =
            await ExecuteCommandAsync("agent", "hooks", "claude", "status", "--scope", "project");

        // assert: status after uninstall - back to missing for every event,
        // which is why this exits non-zero (mirrors `doctor`'s "unhealthy"
        // exit code): status is a check, not just a report.
        Assert.Equal(1, statusAfterUninstall.ExitCode);
        Assert.DoesNotContain("outdated", statusAfterUninstall.StdOut, StringComparison.Ordinal);
        Assert.DoesNotContain("installed", statusAfterUninstall.StdOut, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InstallStatusUninstall_BareVerbAliases_ProjectScope_WriteTheSameEntriesAsTheClaudeGroup()
    {
        // arrange
        var sidecarDirectory = Path.Combine(WorkingDirectory, "..", "app-data");
        SetupGlobalConfigDirectory(sidecarDirectory);
        await InitWorkspaceAsync();

        // act: install through the deprecated bare verb
        var install = await ExecuteCommandAsync("agent", "hooks", "install", "--scope", "project");

        // assert: install still writes the same settings entries the claude
        // group's install writes, and warns on stderr about the alias
        Assert.Equal(0, install.ExitCode);
        Assert.Contains(
            "'nitro agent hooks install' is deprecated; use 'nitro agent hooks claude install' instead.",
            install.StdErr);
        var settingsPath = Path.Combine(WorkingDirectory, ".claude", "settings.json");
        Assert.True(File.Exists(settingsPath));

        // act: status through the deprecated bare verb
        var status = await ExecuteCommandAsync("agent", "hooks", "status", "--scope", "project");

        // assert: status
        Assert.Equal(0, status.ExitCode);
        Assert.Contains(
            "'nitro agent hooks status' is deprecated; use 'nitro agent hooks claude status' instead.",
            status.StdErr);
        Assert.Contains("SessionStart", status.StdOut);
        Assert.DoesNotContain("missing", status.StdOut, StringComparison.Ordinal);

        // act: uninstall through the deprecated bare verb
        var uninstall = await ExecuteCommandAsync("agent", "hooks", "uninstall", "--scope", "project");

        // assert: uninstall
        Assert.Equal(0, uninstall.ExitCode);
        Assert.Contains(
            "'nitro agent hooks uninstall' is deprecated; use 'nitro agent hooks claude uninstall' instead.",
            uninstall.StdErr);
    }
}
