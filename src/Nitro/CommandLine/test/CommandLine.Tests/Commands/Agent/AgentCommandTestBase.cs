using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Commands;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Runs the agent registry commands (register and list) against a real
/// SQLite workspace in a per-test temp directory named "acme".
/// </summary>
public abstract class AgentCommandTestBase : CommandTestBase
{
    private readonly DirectoryInfo _tempRoot;

    protected AgentCommandTestBase(NitroCommandFixture fixture) : base(fixture)
    {
        SetupNoAuthentication();
        SetupActingActor("test-agent");

        _tempRoot = Directory.CreateTempSubdirectory("nitro-agent-registry-tests");
        WorkingDirectory = Path.Combine(_tempRoot.FullName, "acme");
        Directory.CreateDirectory(WorkingDirectory);
        SetupFileSystem(new TestFileSystem(WorkingDirectory));
    }

    protected string WorkingDirectory { get; }

    protected string WorkspaceDirectory
        => AgentWorkspace.GetDirectory(WorkingDirectory);

    protected string DatabasePath
        => AgentWorkspace.GetDatabasePath(WorkspaceDirectory);

    protected async Task SeedAgentAsync(string actor, string role = "")
        => await new AgentRegistry(new TestFileSystem(WorkingDirectory), FakeTime, new AgentDatabase())
            .RegisterAsync(actor, role, client: "", TestContext.Current.CancellationToken);

    protected async Task InsertSessionIdentityAsync(
        string actor,
        string sessionId,
        string harness = AgentSessionHarness.ClaudeCode,
        string role = "")
    {
        await SeedAgentAsync(actor, role);

        await using var connection = new SqliteConnection($"Data Source={DatabasePath};Pooling=False");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            "INSERT INTO agent_session_identities "
            + "(harness, session_id, actor, role, actor_revision, created_at, last_seen_at) "
            + "VALUES ($harness, $sessionId, $actor, $role, 1, $now, $now)";
        command.Parameters.AddWithValue("$harness", harness);
        command.Parameters.AddWithValue("$sessionId", sessionId);
        command.Parameters.AddWithValue("$actor", actor);
        command.Parameters.AddWithValue("$role", role);
        command.Parameters.AddWithValue("$now", FakeTime.GetUtcNow());
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    protected async Task InitWorkspaceAsync()
    {
        var result = await ExecuteCommandAsync("agent", "init");
        Assert.Equal(0, result.ExitCode);
    }

    /// <summary>
    /// Inserts a live <c>agent_sessions</c> row on <paramref name="host"/>
    /// for the current test process's own pid and start time, so the
    /// registry's liveness check reports it alive when <paramref name="host"/>
    /// is the workspace's current instance id. Used to seed presence
    /// scenarios (<c>agent list</c>'s presence column, the TUI Agents tab's
    /// presence badge) without going through the (not-yet-built) hook
    /// adapters.
    /// </summary>
    protected async Task InsertAliveSessionRowAsync(
        string host,
        string sessionId,
        string? agentName,
        string bindingKind = "explicit",
        string harness = "claude-code",
        string endpointKind = "none",
        string endpointAddr = "",
        string role = "",
        string harnessVersion = "")
    {
        using var process = System.Diagnostics.Process.GetCurrentProcess();
        var pid = process.Id;

        // The real /proc-reported start ticks for this pid: registry
        // methods that predicate on the full generation (e.g.
        // TryClaimPingCooldownAsync, and Observe's liveness check) match
        // proc_start with raw string equality against exactly this value.
        var procStart = ProcStat.ReadStartTicks(pid)!;

        await using var connection = new SqliteConnection($"Data Source={DatabasePath};Pooling=False");
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        if (agentName is not null)
        {
            await using var agentCommand = connection.CreateCommand();
            agentCommand.CommandText =
                "INSERT OR IGNORE INTO agents (name, registered_at, last_seen_at) "
                + "VALUES ($name, $now, $now);";
            agentCommand.Parameters.AddWithValue("$name", agentName);
            agentCommand.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow);
            await agentCommand.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO agent_sessions (
                harness, session_id, agent_name, binding_kind, host, pid, proc_start,
                cwd, workspace_path, endpoint_kind, endpoint_addr, started_at, last_beat_at,
                role, harness_version
            ) VALUES (
                $harness, $sessionId, $agentName, $bindingKind, $host, $pid, $procStart,
                '/work', '/work/.nitro/agents', $endpointKind, $endpointAddr, $now, $now,
                $role, $harnessVersion
            );
            """;
        command.Parameters.AddWithValue("$harness", harness);
        command.Parameters.AddWithValue("$sessionId", sessionId);
        command.Parameters.AddWithValue("$agentName", (object?)agentName ?? DBNull.Value);
        command.Parameters.AddWithValue("$bindingKind", bindingKind);
        command.Parameters.AddWithValue("$host", host);
        command.Parameters.AddWithValue("$pid", pid);
        command.Parameters.AddWithValue("$procStart", procStart);
        command.Parameters.AddWithValue("$endpointKind", endpointKind);
        command.Parameters.AddWithValue("$endpointAddr", endpointAddr);
        command.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow);
        command.Parameters.AddWithValue("$role", role);
        command.Parameters.AddWithValue("$harnessVersion", harnessVersion);

        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Updates the mutable role on the session row matching <paramref name="host"/>
    /// and <paramref name="sessionId"/>, standing in for the same-row role
    /// promotion <c>IAgentSessionRegistry.RegisterAsync</c> applies, without
    /// requiring a detectable harness ancestor process in the test.
    /// </summary>
    protected async Task UpdateSessionRoleAsync(string host, string sessionId, string role)
    {
        await using var connection = new SqliteConnection($"Data Source={DatabasePath};Pooling=False");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            "UPDATE agent_sessions SET role = $role WHERE host = $host AND session_id = $sessionId";
        command.Parameters.AddWithValue("$role", role);
        command.Parameters.AddWithValue("$host", host);
        command.Parameters.AddWithValue("$sessionId", sessionId);

        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Inserts an <c>agent_sessions</c> row on <paramref name="host"/> at a
    /// pid that is never alive (999999), so the registry's liveness check
    /// reaps it on the next read when <paramref name="host"/> is the
    /// workspace's current instance id (a remote host's row is never reaped
    /// regardless of pid liveness). Mirrors
    /// <c>ListSessionCommandTests.InsertDeadSessionRowAsync</c>.
    /// </summary>
    protected async Task InsertDeadSessionRowAsync(string host, string sessionId, string? agentName = null)
    {
        await using var connection = new SqliteConnection($"Data Source={DatabasePath};Pooling=False");
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        if (agentName is not null)
        {
            await using var agentCommand = connection.CreateCommand();
            agentCommand.CommandText =
                "INSERT OR IGNORE INTO agents (name, registered_at, last_seen_at) "
                + "VALUES ($name, $now, $now);";
            agentCommand.Parameters.AddWithValue("$name", agentName);
            agentCommand.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow);
            await agentCommand.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO agent_sessions (
                harness, session_id, agent_name, binding_kind, host, pid, proc_start,
                cwd, workspace_path, endpoint_kind, endpoint_addr, started_at, last_beat_at
            ) VALUES (
                'claude-code', $sessionId, $agentName, $bindingKind, $host, 999999, $now,
                '/work', '/work/.nitro/agents', 'none', '', $now, $now
            );
            """;
        command.Parameters.AddWithValue("$sessionId", sessionId);
        command.Parameters.AddWithValue("$agentName", (object?)agentName ?? DBNull.Value);
        command.Parameters.AddWithValue("$bindingKind", agentName is null ? "none" : "explicit");
        command.Parameters.AddWithValue("$host", host);
        command.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow);

        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Runs a scalar query against the workspace database and returns the
    /// first column of the first row as a string.
    /// </summary>
    protected async Task<string?> QueryScalarAsync(string sql)
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var connection =
            new SqliteConnection($"Data Source={DatabasePath};Pooling=False");
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return result is null or DBNull ? null : result.ToString();
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        _tempRoot.Delete(recursive: true);
    }
}
