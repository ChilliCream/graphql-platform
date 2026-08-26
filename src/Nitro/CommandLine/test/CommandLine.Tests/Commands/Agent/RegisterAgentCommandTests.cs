using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Commands.Agent;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

public sealed class RegisterAgentCommandTests(NitroCommandFixture fixture)
    : AgentCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSimplifiedOptions()
    {
        var result = await ExecuteCommandAsync("agent", "register", "--help");

        result.AssertHelpOutput(
            """
            Description:
              Ensure the current session has an actor, or update its actor and role.

            Usage:
              nitro agent register [options]

            Options:
              --actor <actor>  The actor performing this command; inferred from the current session when omitted
              --role <role>    The actor role, normalized lowercase
              --force          Take the actor from another session and remove that session
              --output <json>  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help   Show help and usage information

            Example:
              nitro agent register
              nitro agent register --role "backend"
            """);
    }

    [Fact]
    public async Task Register_RequiresAnIdentifiableSession()
    {
        await InitWorkspaceAsync();

        var result = await ExecuteCommandAsync("agent", "register");

        result.AssertError(
            """
            Could not identify the current Claude, Codex, or Copilot session: no harness process or session id detected. Install Nitro hooks and retry from that session.
            """);
        Assert.Equal("0", await QueryScalarAsync("SELECT COUNT(*) FROM agents"));
    }

    [Theory]
    [InlineData("--client")]
    [InlineData("--force-rebind")]
    public async Task Register_RemovedOptionsAreRejected(string option)
    {
        var result = await ExecuteCommandAsync("agent", "register", option, "value");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains($"Unrecognized command or argument '{option}'", result.StdErr);
    }
}

public sealed class RegisterAgentCommandSessionAwareTests : AgentCommandTestBase
{
    private const string FixedHost = "host-register-session-aware-tests";

    public RegisterAgentCommandSessionAwareTests(NitroCommandFixture fixture) : base(fixture)
    {
        SetupInstanceId(FixedHost);
    }

    [Fact]
    public async Task Claude_BootstrapsAnActorAndSetsRole()
    {
        await InitWorkspaceAsync();
        SetupClaudeAncestor("claude-session-1");

        var result = await ExecuteCommandAsync("agent", "register", "--role", "Orchestrator");

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdErr);
        var actor = await QueryScalarAsync(
            "SELECT actor FROM agent_session_identities WHERE session_id = 'claude-session-1'");
        Assert.NotNull(actor);
        Assert.Contains($"Actor '{actor}', role 'orchestrator'.", result.StdOut);
        Assert.Equal(
            actor,
            await QueryScalarAsync("SELECT agent_name FROM agent_sessions WHERE session_id = 'claude-session-1'"));
        Assert.Equal(
            "orchestrator",
            await QueryScalarAsync("SELECT role FROM agent_session_identities WHERE session_id = 'claude-session-1'"));
    }

    [Fact]
    public async Task Register_ActorAndRoleOverwriteTheCurrentSession()
    {
        await InitWorkspaceAsync();
        SetupClaudeAncestor("claude-session-1");
        await ExecuteCommandAsync("agent", "register");

        var result = await ExecuteCommandAsync(
            "agent", "register", "--actor", "Alice", "--role", "Backend");

        result.AssertSuccess("✓ Actor 'alice', role 'backend'.");
        Assert.Equal(
            "alice|backend|2",
            await QueryScalarAsync(
                "SELECT actor || '|' || role || '|' || actor_revision "
                + "FROM agent_session_identities WHERE session_id = 'claude-session-1'"));
        Assert.Equal(
            "alice|backend|explicit",
            await QueryScalarAsync(
                "SELECT agent_name || '|' || role || '|' || binding_kind "
                + "FROM agent_sessions WHERE session_id = 'claude-session-1'"));
    }

    [Fact]
    public async Task Register_OmittedOptionsPreserveActorAndRole()
    {
        await InitWorkspaceAsync();
        SetupClaudeAncestor("claude-session-1");
        await ExecuteCommandAsync(
            "agent", "register", "--actor", "alice", "--role", "backend");
        SetupInteractionMode(InteractionMode.JsonOutput);

        var result = await ExecuteCommandAsync("agent", "register");

        using var document = JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("alice", root.GetProperty("actor").GetString());
        Assert.Equal("backend", root.GetProperty("role").GetString());
        Assert.False(root.GetProperty("changed").GetBoolean());
        Assert.True(root.GetProperty("connected").GetBoolean());
    }

    [Fact]
    public async Task Codex_UsesTheExistingAuthoritativeSessionRow()
    {
        await InitWorkspaceAsync();
        await InsertSessionRowAsync("codex", "codex-session-1");
        SetupRawEnvironmentVariable("CODEX_SESSION_ID", "codex-session-1");

        var result = await ExecuteCommandAsync("agent", "register", "--role", "worker");

        Assert.Equal(0, result.ExitCode);
        var actor = await QueryScalarAsync(
            "SELECT actor FROM agent_session_identities WHERE session_id = 'codex-session-1'");
        Assert.Contains($"Actor '{actor}', role 'worker'.", result.StdOut);
        Assert.Equal(
            actor,
            await QueryScalarAsync("SELECT agent_name FROM agent_sessions WHERE session_id = 'codex-session-1'"));
    }

    [Fact]
    public async Task Codex_DoesNotCreateAProvisionalIdentityFromProcessOnly()
    {
        await InitWorkspaceAsync();
        using var process = System.Diagnostics.Process.GetCurrentProcess();
        SetupAncestorSessionResolvers(codex: new CodexAncestorSession(process.Id));

        var result = await ExecuteCommandAsync("agent", "register");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("No authoritative codex session is registered", result.StdErr);
        Assert.Equal("0", await QueryScalarAsync("SELECT COUNT(*) FROM agent_sessions"));
        Assert.Equal("0", await QueryScalarAsync("SELECT COUNT(*) FROM agent_session_identities"));
    }

    [Fact]
    public async Task Codex_SessionIdWithoutLiveRowIsRejected()
    {
        await InitWorkspaceAsync();
        SetupRawEnvironmentVariable("CODEX_SESSION_ID", "codex-session-missing");

        var result = await ExecuteCommandAsync("agent", "register");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("has no live session row on this host", result.StdErr);
        Assert.Equal("0", await QueryScalarAsync("SELECT COUNT(*) FROM agents"));
    }

    [Fact]
    public async Task Execute_Should_GenerateProcessLifetimeSessionAndActor_When_CopilotRegisters()
    {
        await InitWorkspaceAsync();
        using var process = System.Diagnostics.Process.GetCurrentProcess();
        SetupAncestorSessionResolvers(copilot: new CopilotAncestorSession(process.Id));

        var result = await ExecuteCommandAsync("agent", "register", "--role", "worker");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(
            "copilot",
            await QueryScalarAsync("SELECT harness FROM agent_session_identities"));
        Assert.StartsWith(
            $"generated:{FixedHost}:{process.Id}:",
            await QueryScalarAsync("SELECT session_id FROM agent_session_identities"));
        Assert.NotNull(await QueryScalarAsync("SELECT actor FROM agent_session_identities"));
    }

    [Fact]
    public async Task SessionEnd_Should_ReleaseEphemeralIdentity_When_CopilotExits()
    {
        await InitWorkspaceAsync();
        using var process = System.Diagnostics.Process.GetCurrentProcess();
        SetupAncestorSessionResolvers(copilot: new CopilotAncestorSession(process.Id));
        await ExecuteCommandAsync("agent", "register");

        var result = await ExecuteCommandAsync("agent", "hook", "copilot", "session-end");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("0", await QueryScalarAsync("SELECT COUNT(*) FROM agent_sessions"));
        Assert.Equal("0", await QueryScalarAsync("SELECT COUNT(*) FROM agent_session_identities"));
    }

    [Fact]
    public async Task JsonOutput_UsesActorTerminologyAndSessionMetadata()
    {
        await InitWorkspaceAsync();
        await InsertSessionRowAsync("codex", "codex-session-1", harnessVersion: "0.101.0");
        SetupRawEnvironmentVariable("CODEX_SESSION_ID", "codex-session-1");
        SetupInteractionMode(InteractionMode.JsonOutput);

        var result = await ExecuteCommandAsync("agent", "register", "--actor", "alice", "--role", "worker");

        using var document = JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("alice", root.GetProperty("actor").GetString());
        Assert.Equal("worker", root.GetProperty("role").GetString());
        Assert.Equal("codex", root.GetProperty("harness").GetString());
        Assert.Equal("codex-session-1", root.GetProperty("sessionId").GetString());
        Assert.Equal("0.101.0", root.GetProperty("harnessVersion").GetString());
        Assert.True(root.GetProperty("changed").GetBoolean());
        Assert.True(root.GetProperty("connected").GetBoolean());
        Assert.False(root.TryGetProperty("name", out _));
        Assert.False(root.TryGetProperty("client", out _));
    }

    [Fact]
    public async Task Register_RejectsAnActorAssignedToAnotherSession()
    {
        await InitWorkspaceAsync();
        SetupClaudeAncestor("claude-session-1");
        await ExecuteCommandAsync("agent", "register", "--actor", "alice");
        SetupClaudeAncestor("claude-session-2");

        var result = await ExecuteCommandAsync("agent", "register", "--actor", "alice");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Actor 'alice' is already assigned to another session.", result.StdErr);
    }

    [Fact]
    public async Task Execute_Should_RequireActor_When_ForceIsSet()
    {
        await InitWorkspaceAsync();

        var result = await ExecuteCommandAsync("agent", "register", "--force");

        result.AssertError("Option '--force' requires '--actor'.");
    }

    [Fact]
    public async Task Execute_Should_MoveActorAndRemoveOtherSession_When_ForceIsSet()
    {
        await InitWorkspaceAsync();
        SetupClaudeAncestor("claude-session-1");
        await ExecuteCommandAsync("agent", "register", "--actor", "alice");
        SetupClaudeAncestor("claude-session-2");
        await ExecuteCommandAsync("agent", "register");

        var result = await ExecuteCommandAsync(
            "agent", "register", "--actor", "alice", "--force");

        result.AssertSuccess("✓ Actor 'alice'.");
        Assert.Equal(
            "claude-session-2",
            await QueryScalarAsync(
                "SELECT session_id FROM agent_session_identities WHERE actor = 'alice'"));
        Assert.Equal(
            "0",
            await QueryScalarAsync(
                "SELECT COUNT(*) FROM agent_sessions WHERE session_id = 'claude-session-1'"));
    }

    [Fact]
    public async Task Execute_Should_AssignFreshActor_When_DisplacedSessionStartsAgain()
    {
        await InitWorkspaceAsync();
        SetupClaudeAncestor("claude-session-1");
        await ExecuteCommandAsync("agent", "register", "--actor", "alice");
        SetupClaudeAncestor("claude-session-2");
        await ExecuteCommandAsync("agent", "register", "--actor", "alice", "--force");
        SetupClaudeAncestor("claude-session-1");

        var result = await ExecuteCommandAsync("agent", "register");

        Assert.Equal(0, result.ExitCode);
        var actor = await QueryScalarAsync(
            "SELECT actor FROM agent_session_identities WHERE session_id = 'claude-session-1'");
        Assert.NotNull(actor);
        Assert.NotEqual("alice", actor);
    }

    [Fact]
    public async Task Execute_Should_ClearRole_When_ExplicitRoleIsEmpty()
    {
        await InitWorkspaceAsync();
        SetupClaudeAncestor("claude-session-1");
        await ExecuteCommandAsync("agent", "register", "--role", "planner");

        var result = await ExecuteCommandAsync("agent", "register", "--role", "");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(
            "",
            await QueryScalarAsync(
                "SELECT role FROM agent_session_identities WHERE session_id = 'claude-session-1'"));
    }

    private void SetupClaudeAncestor(string sessionId)
    {
        using var process = System.Diagnostics.Process.GetCurrentProcess();
        SetupAncestorSessionResolvers(
            claude: new ClaudeAncestorSession(process.Id, sessionId, WorkingDirectory, "peer-a"));
    }

    private async Task InsertSessionRowAsync(
        string harness,
        string sessionId,
        string harnessVersion = "")
    {
        using var process = System.Diagnostics.Process.GetCurrentProcess();
        var procStart = ProcStat.ReadStartTicks(process.Id)!;

        await using var connection = new SqliteConnection($"Data Source={DatabasePath};Pooling=False");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO agent_sessions (
                harness, session_id, agent_name, binding_kind, host, pid, proc_start,
                cwd, workspace_path, endpoint_kind, endpoint_addr, started_at, last_beat_at,
                harness_version
            ) VALUES (
                $harness, $sessionId, NULL, 'none', $host, $pid, $procStart,
                $cwd, $workspacePath, 'none', '', $now, $now, $harnessVersion
            );
            """;
        command.Parameters.AddWithValue("$harness", harness);
        command.Parameters.AddWithValue("$sessionId", sessionId);
        command.Parameters.AddWithValue("$host", FixedHost);
        command.Parameters.AddWithValue("$pid", process.Id);
        command.Parameters.AddWithValue("$procStart", procStart);
        command.Parameters.AddWithValue("$cwd", WorkingDirectory);
        command.Parameters.AddWithValue("$workspacePath", WorkspaceDirectory);
        command.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow);
        command.Parameters.AddWithValue("$harnessVersion", harnessVersion);

        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }
}
