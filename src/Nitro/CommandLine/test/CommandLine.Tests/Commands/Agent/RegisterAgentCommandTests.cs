using ChilliCream.Nitro.CommandLine.Commands.Agent;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Exercises the identity-only fallback (no harness ancestor detected, this
/// command's original behavior, and <see cref="AgentCommandTestBase"/>'s
/// default) and command wiring. The session-aware registration path (all
/// three harnesses, the claim state machine, role promotion) is exercised
/// directly against <see cref="AgentSessionRegistry"/> in
/// <c>AgentSessionRegistryTests</c>, and the harness-resolution branching
/// itself in <see cref="RegisterAgentCommandSessionAwareTests"/>.
/// </summary>
public sealed class RegisterAgentCommandTests(NitroCommandFixture fixture)
    : AgentCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "register", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Register the resolved actor as an agent, with an optional role. --actor is per invocation; set NITRO_MAIL_ACTOR to persist an identity.

            Usage:
              nitro agent register [options]

            Options:
              --actor <actor>    The acting identity used on mail commands (defaults to NITRO_MAIL_ACTOR, NITRO_TASK_ACTOR, or the OS user name)
              --role <role>      The agent's role, free text, normalized lowercase (defaults to empty)
              --client <client>  The client program the agent runs as, e.g. "claude-code" or "codex", free text, normalized lowercase (defaults to auto-detected, or empty)
              --force-rebind     Reclaim a session already explicitly claimed by a different actor, resetting its delivery ledger and block budget
              --output <json>    The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help     Show help and usage information

            Example:
              nitro agent register
              nitro agent register --role "backend"
            """);
    }

    [Fact]
    public async Task DefaultActor_RegistersAgent()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "register");

        // assert
        result.AssertSuccess(
            """
            ✓ Registered 'test-agent'.
            """);
        Assert.Equal(
            "test-agent",
            await QueryScalarAsync("SELECT name FROM agents WHERE name = 'test-agent'"));
    }

    [Fact]
    public async Task RoleOption_RegistersAgentWithRole()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "register", "--role", "Backend");

        // assert
        result.AssertSuccess(
            """
            ✓ Registered 'test-agent' as 'backend'.
            """);
        Assert.Equal(
            "backend",
            await QueryScalarAsync("SELECT role FROM agents WHERE name = 'test-agent'"));
    }

    [Fact]
    public async Task ClientOption_RegistersAgentWithClient()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "register", "--client", "Claude-Code");

        // assert
        result.AssertSuccess(
            """
            ✓ Registered 'test-agent'.
            """);
        Assert.Equal(
            "claude-code",
            await QueryScalarAsync("SELECT client FROM agents WHERE name = 'test-agent'"));
    }

    [Fact]
    public async Task NoClientOptionAndNoMarker_RegistersAgentWithEmptyClient()
    {
        // arrange: no --client given, and the mocked environment carries no
        // CLAUDECODE (or any other) marker, so DetectClient finds nothing.
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "register");

        // assert
        result.AssertSuccess(
            """
            ✓ Registered 'test-agent'.
            """);
        Assert.Equal(
            "",
            await QueryScalarAsync("SELECT client FROM agents WHERE name = 'test-agent'"));
    }

    [Fact]
    public async Task ActorOption_OverridesEnvironmentVariable()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync(
            "agent", "register", "--actor", "Explicit-Agent");

        // assert
        result.AssertSuccess(
            """
            ✓ Registered 'explicit-agent'.
            """);
    }

    [Fact]
    public async Task JsonOutput_ReturnsRegisteredAgent()
    {
        // arrange
        await InitWorkspaceAsync();
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "register", "--role", "backend", "--client", "codex");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;

        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("test-agent", root.GetProperty("name").GetString());
        Assert.Equal("backend", root.GetProperty("role").GetString());
        Assert.Equal("codex", root.GetProperty("client").GetString());
        Assert.True(root.TryGetProperty("registeredAt", out _));
        Assert.True(root.TryGetProperty("lastSeenAt", out _));
    }

    [Fact]
    public async Task Reregister_IsIdempotent_AndUpdatesLastSeenAt()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register");
        var registeredAt = await QueryScalarAsync(
            "SELECT registered_at FROM agents WHERE name = 'test-agent'");
        FakeTime.Advance(TimeSpan.FromMinutes(5));

        // act
        var result = await ExecuteCommandAsync("agent", "register");

        // assert
        result.AssertSuccess(
            """
            ✓ Registered 'test-agent'.
            """);
        Assert.Equal(
            1L,
            long.Parse(
                (await QueryScalarAsync("SELECT COUNT(*) FROM agents WHERE name = 'test-agent'"))!));
        Assert.Equal(
            registeredAt,
            await QueryScalarAsync("SELECT registered_at FROM agents WHERE name = 'test-agent'"));
        Assert.NotEqual(
            registeredAt,
            await QueryScalarAsync("SELECT last_seen_at FROM agents WHERE name = 'test-agent'"));
    }

    [Fact]
    public async Task Reregister_WithoutRole_ClearsPreviousRole()
    {
        // arrange: register sets role on every call, defaulting to empty
        // when --role is omitted, the same way last_seen_at is always
        // bumped; only the mail-send auto-registration path (TouchAsync)
        // preserves an existing role.
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--role", "backend");

        // act
        var result = await ExecuteCommandAsync("agent", "register");

        // assert
        result.AssertSuccess(
            """
            ✓ Registered 'test-agent'.
            """);
        Assert.Equal(
            "",
            await QueryScalarAsync("SELECT role FROM agents WHERE name = 'test-agent'"));
    }

    [Fact]
    public async Task Reregister_WithoutClient_ClearsPreviousClient()
    {
        // arrange: register sets client on every call, defaulting to
        // auto-detected-or-empty when --client is omitted, the same way
        // role and last_seen_at are always overwritten.
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--client", "claude-code");

        // act
        var result = await ExecuteCommandAsync("agent", "register");

        // assert
        result.AssertSuccess(
            """
            ✓ Registered 'test-agent'.
            """);
        Assert.Equal(
            "",
            await QueryScalarAsync("SELECT client FROM agents WHERE name = 'test-agent'"));
    }

    [Fact]
    public async Task NoWorkspace_ReturnsError()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "register");

        // assert
        result.AssertError(
            """
            No agent workspace found. Run `nitro agent init` first.
            """);
    }

    [Theory]
    [InlineData("agent.mail")]
    [InlineData("agent name")]
    public async Task InvalidActor_ReturnsError(string actor)
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "register", "--actor", actor);

        // assert
        Assert.Empty(result.StdOut);
        Assert.Equal(1, result.ExitCode);
        Assert.Contains(actor, result.StdErr);
    }

    [Fact]
    public async Task EmptyActor_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "register", "--actor", "");

        // assert
        result.AssertError(
            """
            An agent name must not be empty.
            """);
    }
}

/// <summary>
/// Exercises register's session-aware path across all three harnesses via
/// fixed ancestor resolvers and a fixed instance id: the harness-resolution
/// branching (Claude by its ancestor session's own id, Codex and Copilot by
/// (host, pid, proc_start) since their ancestor resolvers expose no session
/// file), the wrong-workspace guard, and the machine-readable output shape.
/// The claim state machine and role promotion themselves are exercised
/// directly against <see cref="AgentSessionRegistry"/> in
/// <c>AgentSessionRegistryTests</c>.
/// </summary>
public sealed class RegisterAgentCommandSessionAwareTests : AgentCommandTestBase
{
    private const string FixedHost = "host-register-session-aware-tests";

    public RegisterAgentCommandSessionAwareTests(NitroCommandFixture fixture) : base(fixture)
    {
        SetupInstanceId(FixedHost);
    }

    [Fact]
    public async Task Claude_RegistersAndBindsTheHookStartedSession()
    {
        // arrange
        await InitWorkspaceAsync();
        using var process = System.Diagnostics.Process.GetCurrentProcess();
        await InsertSessionRowAsync("claude-code", "claude-session-1");
        SetupAncestorSessionResolvers(
            claude: new ClaudeAncestorSession(process.Id, "claude-session-1", WorkingDirectory, "peer-a"));

        // act
        var result = await ExecuteCommandAsync("agent", "register", "--role", "orchestrator");

        // assert
        result.AssertSuccess(
            """
            ✓ Registered 'test-agent' as 'orchestrator', bound to claude-code session 'claude-session-1'.
            """);
        Assert.Equal(
            "test-agent",
            await QueryScalarAsync("SELECT agent_name FROM agent_sessions WHERE session_id = 'claude-session-1'"));
        Assert.Equal(
            "orchestrator",
            await QueryScalarAsync("SELECT role FROM agent_sessions WHERE session_id = 'claude-session-1'"));
    }

    [Fact]
    public async Task Codex_RegistersAndBindsTheHookStartedSession()
    {
        // arrange
        await InitWorkspaceAsync();
        using var process = System.Diagnostics.Process.GetCurrentProcess();
        await InsertSessionRowAsync("codex", "codex-session-1");
        SetupAncestorSessionResolvers(codex: new CodexAncestorSession(process.Id));

        // act
        var result = await ExecuteCommandAsync("agent", "register", "--role", "worker");

        // assert
        result.AssertSuccess(
            """
            ✓ Registered 'test-agent' as 'worker', bound to codex session 'codex-session-1'.
            """);
    }

    [Fact]
    public async Task Copilot_RegistersAndBindsTheHookStartedSession()
    {
        // arrange
        await InitWorkspaceAsync();
        using var process = System.Diagnostics.Process.GetCurrentProcess();
        await InsertSessionRowAsync("copilot", "copilot-session-1");
        SetupAncestorSessionResolvers(copilot: new CopilotAncestorSession(process.Id));

        // act
        var result = await ExecuteCommandAsync("agent", "register", "--role", "worker");

        // assert
        result.AssertSuccess(
            """
            ✓ Registered 'test-agent' as 'worker', bound to copilot session 'copilot-session-1'.
            """);
    }

    [Fact]
    public async Task ReturnsError_When_TheAncestorsWorkspaceDoesNotMatch()
    {
        // arrange
        await InitWorkspaceAsync();
        using var process = System.Diagnostics.Process.GetCurrentProcess();
        var otherRoot = Directory.CreateTempSubdirectory("nitro-register-other-workspace-tests");

        try
        {
            SetupAncestorSessionResolvers(
                claude: new ClaudeAncestorSession(process.Id, "claude-session-1", otherRoot.FullName, "peer-a"));

            // act
            var result = await ExecuteCommandAsync("agent", "register");

            // assert
            Assert.Equal(1, result.ExitCode);
            Assert.Contains("No agent workspace found", result.StdErr);
        }
        finally
        {
            otherRoot.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task ReturnsError_When_NoSessionRowExistsForTheAncestor()
    {
        // arrange: the missing-row case through the CLI - an ancestor was
        // detected but no SessionStart hook has fired for it yet.
        await InitWorkspaceAsync();
        using var process = System.Diagnostics.Process.GetCurrentProcess();
        SetupAncestorSessionResolvers(codex: new CodexAncestorSession(process.Id));

        // act
        var result = await ExecuteCommandAsync("agent", "register");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("No codex session found", result.StdErr);
    }

    [Fact]
    public async Task ReturnsError_When_TheAncestorsProcessMatchesMoreThanOneSession()
    {
        // arrange: two codex rows share this exact (host, pid, proc-start) -
        // register cannot pick one to bind.
        await InitWorkspaceAsync();
        using var process = System.Diagnostics.Process.GetCurrentProcess();
        await InsertSessionRowAsync("codex", "codex-session-1");
        await InsertSessionRowAsync("codex", "codex-session-2");
        SetupAncestorSessionResolvers(codex: new CodexAncestorSession(process.Id));

        // act
        var result = await ExecuteCommandAsync("agent", "register");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains(
            $"Found 2 ambiguous codex sessions for pid {process.Id} on this host.", result.StdErr);
    }

    [Fact]
    public async Task JsonOutput_IncludesHarnessSessionVersionAndRole()
    {
        // arrange
        await InitWorkspaceAsync();
        using var process = System.Diagnostics.Process.GetCurrentProcess();
        await InsertSessionRowAsync("codex", "codex-session-1");
        SetupAncestorSessionResolvers(codex: new CodexAncestorSession(process.Id));
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "register", "--role", "worker");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("codex", root.GetProperty("harness").GetString());
        Assert.Equal("codex-session-1", root.GetProperty("sessionId").GetString());
        Assert.Equal("worker", root.GetProperty("role").GetString());
        Assert.True(root.TryGetProperty("harnessVersion", out _));
    }

    private async Task InsertSessionRowAsync(string harness, string sessionId)
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
                cwd, workspace_path, endpoint_kind, endpoint_addr, started_at, last_beat_at
            ) VALUES (
                $harness, $sessionId, NULL, 'none', $host, $pid, $procStart,
                $cwd, $workspacePath, 'none', '', $now, $now
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

        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }
}

/// <summary>
/// Exercises <see cref="RegisterAgentCommand.DetectClient"/> directly,
/// against a hand-rolled <see cref="IEnvironmentVariableProvider"/> rather
/// than through the CLI: the mocked provider the other command tests share
/// only stubs names under a "NITRO_" prefix
/// (<see cref="ChilliCream.Nitro.CommandLine.Tests.Commands.CommandTestBase.SetupEnvironmentVariable"/>),
/// which cannot represent an unprefixed marker like CLAUDECODE.
/// </summary>
public sealed class RegisterAgentCommandDetectClientTests
{
    [Fact]
    public void DetectClient_Should_ReturnClaudeCode_When_CLAUDECODEIsSet()
    {
        // arrange
        var environmentVariables = new FakeEnvironmentVariableProvider(
            new Dictionary<string, string> { ["CLAUDECODE"] = "1" });

        // act
        var client = RegisterAgentCommand.DetectClient(environmentVariables);

        // assert
        Assert.Equal("claude-code", client);
    }

    [Fact]
    public void DetectClient_Should_ReturnNull_When_NoKnownMarkerIsSet()
    {
        // arrange
        var environmentVariables = new FakeEnvironmentVariableProvider(new Dictionary<string, string>());

        // act
        var client = RegisterAgentCommand.DetectClient(environmentVariables);

        // assert
        Assert.Null(client);
    }

    private sealed class FakeEnvironmentVariableProvider(IReadOnlyDictionary<string, string> variables)
        : IEnvironmentVariableProvider
    {
        public string? GetEnvironmentVariable(string name)
            => variables.GetValueOrDefault(name);
    }
}
