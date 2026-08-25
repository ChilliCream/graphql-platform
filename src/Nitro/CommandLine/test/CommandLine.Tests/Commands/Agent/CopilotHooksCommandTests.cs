namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Command wiring (help text) only for <c>agent hooks copilot
/// install/status/uninstall</c>. The extension's file behavior is exercised
/// directly against <c>CopilotExtensionInstallerService</c> in its dedicated
/// temp-directory-driven test class.
/// </summary>
public sealed class CopilotHooksCommandTests(NitroCommandFixture fixture) : AgentCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_Hooks_ListsCopilotAlongsideTheExistingClaudeAndCodexHarnesses()
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
              Install, inspect, and remove the nitro-mail Copilot CLI extension asset.

            Usage:
              nitro agent hooks copilot [command] [options]

            Options:
              -?, -h, --help  Show help and usage information

            Commands:
              install    Add or update the nitro-mail Copilot CLI extension asset.
              status     Show whether the nitro-mail Copilot CLI extension asset is missing, current, outdated, or unrecognized.
              uninstall  Remove the nitro-mail Copilot CLI extension asset and its config.
            """);
    }

    [Fact]
    public async Task Help_HooksCopilotInstall_ReturnsSuccess()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "copilot", "install", "--help");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Add or update the nitro-mail Copilot CLI extension asset.", result.StdOut);
        Assert.Contains("--scope", result.StdOut);
        Assert.Contains("--force", result.StdOut);
    }

    [Fact]
    public async Task Help_HooksCopilotStatus_ReturnsSuccess()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "copilot", "status", "--help");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains(
            "Show whether the nitro-mail Copilot CLI extension asset is missing, current, outdated, or unrecognized.", result.StdOut);
    }

    [Fact]
    public async Task Help_HooksCopilotUninstall_ReturnsSuccess()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "copilot", "uninstall", "--help");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Remove the nitro-mail Copilot CLI extension asset and its config.", result.StdOut);
    }

    [Fact]
    public async Task ExtensionInstall_MissingScope_FailsWithAMissingOptionError()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "copilot", "install");

        // assert
        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("--scope", result.StdErr);
    }

    [Fact]
    public async Task ExtensionInstall_ScopeUser_IsRejectedAtParseTime()
    {
        // act
        var result = await ExecuteCommandAsync(
            "agent", "hooks", "copilot", "install", "--scope", "user");

        // assert
        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("project", result.StdErr, StringComparison.Ordinal);
    }
}
