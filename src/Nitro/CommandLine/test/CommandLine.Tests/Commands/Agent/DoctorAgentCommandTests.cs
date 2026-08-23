using System.Diagnostics;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Exercises <c>agent doctor</c>'s static checks: schema version, orphaned
/// or unclaimed session rows, dead-generation rows pending reap, and
/// mixed-instance rows with the explicit <c>--clean-mixed-instance</c>
/// cleanup. All rows are read directly, never through
/// <see cref="IAgentSessionRegistry.ListAsync"/>, so these tests also
/// confirm that a plain `agent doctor` run never mutates or reaps anything
/// on its own (the ticket's "no fixes beyond the explicit mixed-instance
/// cleanup" non-goal).
/// </summary>
public sealed class DoctorAgentCommandTests : AgentCommandTestBase
{
    private const string FixedHost = "host-doctor-tests";

    public DoctorAgentCommandTests(NitroCommandFixture fixture) : base(fixture)
    {
        SetupInstanceId(FixedHost);
    }

    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "doctor", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Check the agent workspace's schema and session presence for problems.

            Usage:
              nitro agent doctor [options]

            Options:
              --clean-mixed-instance  Delete session rows stranded from a previous Nitro instance id (a regenerated fallback id, or a different host sharing this workspace); these rows are never reaped automatically
              --output <json>         The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help          Show help and usage information

            Example:
              nitro agent doctor
              nitro agent doctor --clean-mixed-instance
            """);
    }

    [Fact]
    public async Task NoWorkspace_ReturnsError()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "doctor");

        // assert
        result.AssertError(
            """
            No agent workspace found. Run `nitro agent init` first.
            """);
    }

    [Fact]
    public async Task HealthyWorkspace_NoSessions_ReturnsSuccess()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "doctor");

        // assert
        result.AssertSuccess(
            $"""
            Workspace: {WorkspaceDirectory}
            Schema: v{AgentDatabase.CurrentVersion} (current)

            ✓ Schema version
            ✓ Mixed-instance sessions
            """);
    }

    [Fact]
    public async Task JsonOutput_HealthyWorkspace_ReturnsStructuredReport()
    {
        // arrange
        await InitWorkspaceAsync();
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "doctor");

        // assert
        Assert.Equal(0, result.ExitCode);
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;
        Assert.Equal(WorkspaceDirectory, root.GetProperty("workspacePath").GetString());
        Assert.Equal(AgentDatabase.CurrentVersion, root.GetProperty("schemaVersion").GetInt64());
        Assert.True(root.GetProperty("schemaCurrent").GetBoolean());
        Assert.True(root.GetProperty("healthy").GetBoolean());
        Assert.Empty(root.GetProperty("unclaimedSessions").EnumerateArray());
        Assert.Empty(root.GetProperty("deadGenerationSessions").EnumerateArray());
        Assert.Empty(root.GetProperty("mixedInstanceSessions").EnumerateArray());
        Assert.Equal(0, root.GetProperty("mixedInstanceSessionsCleaned").GetInt32());
    }

    [Fact]
    public async Task UpgradableSchema_ReportedAndReturnsError_SessionChecksSkipped()
    {
        // arrange: a v3-shaped database, mirroring an existing workspace
        // from before the session tables shipped (this repo's own
        // .nitro/agents/ at the time this bead was written).
        await SeedLegacySchemaVersionAsync(3);

        // act
        var result = await ExecuteCommandAsync("agent", "doctor");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("FAIL Schema version:", result.StdOut);
        Assert.Contains("upgradable; run `nitro agent init` to migrate", result.StdOut);
        Assert.Contains("Session checks skipped: the schema is not current.", result.StdOut);
    }

    [Fact]
    public async Task NewerSchema_ReportedAndReturnsError()
    {
        // arrange
        await SeedLegacySchemaVersionAsync(AgentDatabase.CurrentVersion + 1);

        // act
        var result = await ExecuteCommandAsync("agent", "doctor");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("FAIL Schema version:", result.StdOut);
        Assert.Contains("newer than this CLI supports", result.StdOut);
    }

    [Fact]
    public async Task UnclaimedAliveSession_ReportedAsWarning_DoesNotFailHealth()
    {
        // arrange
        await InitWorkspaceAsync();
        await InsertSessionRowAsync(
            FixedHost, "session-1", agentName: null, bindingKind: "none", pid: CurrentAlivePid());

        // act
        var result = await ExecuteCommandAsync("agent", "doctor");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("WARN Unclaimed sessions (informational, no action needed):", result.StdOut);
        Assert.Contains("session-1", result.StdOut);
        Assert.DoesNotContain("WARN Dead-generation", result.StdOut);
    }

    [Fact]
    public async Task DeadGenerationSession_ReportedAsWarning_DoesNotFailHealth_And_IsNotReaped()
    {
        // arrange
        await InitWorkspaceAsync();
        await InsertSessionRowAsync(
            FixedHost, "session-dead", agentName: null, bindingKind: "none", pid: DeadPid);

        // act
        var result = await ExecuteCommandAsync("agent", "doctor");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains(
            "WARN Dead-generation sessions pending reap "
            + "(run `nitro agent session list` to clean up):",
            result.StdOut);
        Assert.Contains("session-dead", result.StdOut);

        // doctor is read-only for anything short of the explicit
        // mixed-instance cleanup: the dead row must still be there.
        var remaining = await QueryScalarAsync(
            "SELECT COUNT(*) FROM agent_sessions WHERE session_id = 'session-dead'");
        Assert.Equal("1", remaining);
    }

    [Fact]
    public async Task MixedInstanceSession_ReportedAndReturnsError_WithoutCleanFlag()
    {
        // arrange
        await InitWorkspaceAsync();
        await InsertSessionRowAsync(
            "some-other-host", "session-remote", agentName: null, bindingKind: "none", pid: 12345);

        // act
        var result = await ExecuteCommandAsync("agent", "doctor");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("FAIL Mixed-instance sessions:", result.StdOut);
        Assert.Contains("session-remote", result.StdOut);
        Assert.Contains("Rerun with --clean-mixed-instance to delete these rows.", result.StdOut);

        var remaining = await QueryScalarAsync(
            "SELECT COUNT(*) FROM agent_sessions WHERE session_id = 'session-remote'");
        Assert.Equal("1", remaining);
    }

    [Fact]
    public async Task MixedInstanceSession_CleanedWithFlag_ReturnsSuccess_And_DeletesOnlyThatRow()
    {
        // arrange
        await InitWorkspaceAsync();
        await InsertSessionRowAsync(
            "some-other-host", "session-remote", agentName: null, bindingKind: "none", pid: 12345);
        await InsertSessionRowAsync(
            FixedHost, "session-local", agentName: null, bindingKind: "none", pid: CurrentAlivePid());

        // act
        var result = await ExecuteCommandAsync("agent", "doctor", "--clean-mixed-instance");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("✓ Mixed-instance sessions", result.StdOut);
        Assert.Contains("Cleaned 1 mixed-instance row.", result.StdOut);

        var remainingRemote = await QueryScalarAsync(
            "SELECT COUNT(*) FROM agent_sessions WHERE session_id = 'session-remote'");
        Assert.Equal("0", remainingRemote);

        var remainingLocal = await QueryScalarAsync(
            "SELECT COUNT(*) FROM agent_sessions WHERE session_id = 'session-local'");
        Assert.Equal("1", remainingLocal);
    }

    [Fact]
    public async Task CleanFlag_NoMixedInstanceRows_IsNoOp()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "doctor", "--clean-mixed-instance");

        // assert
        result.AssertSuccess(
            $"""
            Workspace: {WorkspaceDirectory}
            Schema: v{AgentDatabase.CurrentVersion} (current)

            ✓ Schema version
            ✓ Mixed-instance sessions
            """);
    }

    /// <summary>
    /// A pid guaranteed to be alive for the duration of the test: the test
    /// host process itself, mirroring the convention in
    /// <c>AgentSessionRegistryTests</c> and <c>ListSessionCommandTests</c>.
    /// </summary>
    private static int CurrentAlivePid() => Environment.ProcessId;

    /// <summary>
    /// A pid that (barring extraordinary pid-space exhaustion) belongs to no
    /// running process, so it is reported dead.
    /// </summary>
    private const int DeadPid = 999_999;

    private async Task InsertSessionRowAsync(
        string host, string sessionId, string? agentName, string bindingKind, int pid)
    {
        var procStart = pid == CurrentAlivePid()
            ? Process.GetCurrentProcess().StartTime.ToUniversalTime()
            : DateTimeOffset.UtcNow;

        await using var connection = new SqliteConnection($"Data Source={DatabasePath};Pooling=False");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO agent_sessions (
                harness, session_id, agent_name, binding_kind, host, pid, proc_start,
                cwd, workspace_path, endpoint_kind, endpoint_addr, started_at, last_beat_at
            ) VALUES (
                'claude-code', $sessionId, $agentName, $bindingKind, $host, $pid, $procStart,
                '/work', '/work/.nitro/agents', 'none', '', $now, $now
            );
            """;
        command.Parameters.AddWithValue("$sessionId", sessionId);
        command.Parameters.AddWithValue("$agentName", (object?)agentName ?? DBNull.Value);
        command.Parameters.AddWithValue("$bindingKind", bindingKind);
        command.Parameters.AddWithValue("$host", host);
        command.Parameters.AddWithValue("$pid", pid);
        command.Parameters.AddWithValue("$procStart", procStart);
        command.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow);

        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Creates the workspace directory and a bare database file stamped
    /// with the given <c>user_version</c>, without running a real `agent
    /// init`, mirroring how <c>AgentDatabaseTests</c> seeds a legacy schema
    /// version. Doctor's version check only reads the pragma, so no table
    /// needs to exist for these tests.
    /// </summary>
    private async Task SeedLegacySchemaVersionAsync(long version)
    {
        Directory.CreateDirectory(WorkspaceDirectory);

        await using var connection = new SqliteConnection($"Data Source={DatabasePath};Pooling=False");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA user_version = {version};";
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }
}
