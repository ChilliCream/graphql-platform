using ChilliCream.Nitro.CommandLine.Services.Workspace;

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
    public async Task Help_Hooks_ListsCopilotAlongsideTheExistingHarnesses()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "--help");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("claude", result.StdOut);
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

/// <summary>
/// Covers <c>agent hook copilot</c>, the lifecycle adapter. Copilot CLI has a
/// single event, session-end: there is no session-start hook, so a Copilot
/// agent never receives an allocated actor from a hook and takes the
/// <c>agent login</c> plus <c>agent register --actor</c> path instead, which
/// the last two tests cover.
/// </summary>
public sealed class CopilotHookCommandTests : AgentCommandTestBase
{
    private const string FixedHost = "host-copilot-hook-tests";

    public CopilotHookCommandTests(NitroCommandFixture fixture) : base(fixture)
    {
        SetupInstanceId(FixedHost);
    }

    [Fact]
    public async Task CopilotHelp_Should_ListSessionEnd_AsTheOnlyEvent()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "hook", "copilot", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Adapt Copilot CLI lifecycle events.

            Usage:
              nitro agent hook copilot [command] [options]

            Options:
              -?, -h, --help  Show help and usage information

            Commands:
              session-end  Release the generated Copilot session identity.
            """);
    }

    [Fact]
    public async Task SessionStart_Should_BeRejected_When_InvokedForCopilot()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "hook", "copilot", "session-start");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Unrecognized command or argument 'session-start'", result.StdErr);
    }

    [Fact]
    public async Task SessionEnd_Should_DeleteTheSessionRow_When_ACopilotAncestorResolves()
    {
        // arrange
        await InitWorkspaceAsync();
        await InsertSessionIdentityAsync("maya", "copilot-1", AgentSessionHarness.Copilot);
        await InsertAliveSessionRowAsync(
            FixedHost, "copilot-1", "maya", harness: AgentSessionHarness.Copilot);
        SetupAncestorSessionResolvers(copilot: new CopilotAncestorSession(Environment.ProcessId));

        // act
        var result = await ExecuteCommandAsync("agent", "hook", "copilot", "session-end");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdOut);
        Assert.Equal("0", await QueryScalarAsync("SELECT COUNT(*) FROM agent_sessions"));
    }

    [Fact]
    public async Task SessionEnd_Should_ReleaseTheIdentity_ButKeepTheActor()
    {
        // arrange
        await InitWorkspaceAsync();
        await InsertSessionIdentityAsync("maya", "copilot-1", AgentSessionHarness.Copilot);
        await InsertAliveSessionRowAsync(
            FixedHost, "copilot-1", "maya", harness: AgentSessionHarness.Copilot);
        SetupAncestorSessionResolvers(copilot: new CopilotAncestorSession(Environment.ProcessId));

        // act
        var result = await ExecuteCommandAsync("agent", "hook", "copilot", "session-end");

        // assert: the session identity goes, the allocated actor name stays.
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("0", await QueryScalarAsync("SELECT COUNT(*) FROM agent_session_identities"));
        Assert.Equal("maya", await QueryScalarAsync("SELECT name FROM agents"));
    }

    [Fact]
    public async Task SessionEnd_Should_LeaveTheSessionRow_When_NoCopilotAncestorResolves()
    {
        // arrange: no ancestor resolves, so nothing identifies this process
        // as the Copilot session the seeded row belongs to.
        await InitWorkspaceAsync();
        await InsertSessionIdentityAsync("maya", "copilot-1", AgentSessionHarness.Copilot);
        await InsertAliveSessionRowAsync(
            FixedHost, "copilot-1", "maya", harness: AgentSessionHarness.Copilot);

        // act
        var result = await ExecuteCommandAsync("agent", "hook", "copilot", "session-end");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdOut);
        Assert.Equal("copilot-1", await QueryScalarAsync("SELECT session_id FROM agent_sessions"));
    }

    [Fact]
    public async Task Login_Should_MintAnActor_ThatRegisterAccepts_When_NoSessionResolves()
    {
        // arrange: Copilot has no session-start hook, so the actor is minted
        // by `agent login` and named on every later command.
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "login");
        var actor = await QueryScalarAsync("SELECT name FROM agents");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "register", "--actor", actor!, "--role", "worker");

        // assert: the actor registers alone, bound to no session.
        result.AssertSuccess($"✓ Actor '{actor}', role 'worker'.");
        Assert.Equal(
            "worker",
            await QueryScalarAsync($"SELECT role FROM agents WHERE name = '{actor}'"));
        Assert.Equal("0", await QueryScalarAsync("SELECT COUNT(*) FROM agent_session_identities"));
    }

    [Fact]
    public async Task Register_Should_Fail_When_TheActorWasNeverAllocated_AndNoSessionResolves()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "register", "--actor", "invented");

        // assert
        result.AssertError(
            "Unknown actor 'invented'. Run `nitro agent login` to allocate one, or "
            + "`nitro agent list` to see the actors this workspace knows.");
        Assert.Equal("0", await QueryScalarAsync("SELECT COUNT(*) FROM agents"));
    }
}
