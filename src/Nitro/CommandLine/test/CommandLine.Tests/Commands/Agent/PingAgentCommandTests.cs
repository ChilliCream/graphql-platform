using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Hook;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Runs <c>nitro agent ping</c> against a real workspace database. The
/// codex-thread transport call goes through a fake <c>ICodexQueueClient</c>
/// substituted via <c>SetupCodexQueueClient</c>, so no scenario here ever
/// shells out to a real <c>codex</c> binary. The cooldown/capacity/mutual
/// exclusion coverage here exercises <c>ISessionGateCoordinator</c> only
/// through this command's own observable outcomes (its unit-level
/// correctness is <c>SessionGateCoordinatorTests</c>'s job); "daemon-shaped
/// ownership" is simulated by inserting a <c>session_ping_gates</c> row
/// directly, standing in for a concurrent foreground-mail dispatch or a
/// future out-of-process daemon without needing a second live process.
/// </summary>
public sealed class PingAgentCommandTests : AgentCommandTestBase
{
    private const string FixedHost = "host-ping-agent-tests";

    public PingAgentCommandTests(NitroCommandFixture fixture) : base(fixture)
    {
        SetupInstanceId(FixedHost);
    }

    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "ping", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Fire the best-effort wake ping at every live session an agent has claimed.

            Usage:
              nitro agent ping <actor> [options]

            Arguments:
              <actor>  The recipient agent name to ping

            Options:
              --output <json>  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help   Show help and usage information

            Example:
              nitro agent ping codex-worker-1
            """);
    }

    [Fact]
    public async Task NoLiveSessions_PrintsMessage()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");

        // act
        var result = await ExecuteCommandAsync("agent", "ping", "bob");

        // assert
        result.AssertSuccess("No live sessions for 'bob'.");
    }

    [Fact]
    public async Task JsonOutput_NoLiveSessions_ReturnsEmptyArray()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "ping", "bob");

        // assert
        result.AssertSuccess(
            """
            {
              "items": []
            }
            """);
    }

    [Fact]
    public async Task ClaudePeerSession_SendsTheDigestAndRecordsOk()
    {
        // arrange
        var peerClient = new FakeClaudePeerClient();
        SetupClaudePeerClient(peerClient);
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");
        await InsertAliveSessionRowAsync(
            FixedHost, "session-1", agentName: "bob", endpointKind: "claude-peer", endpointAddr: "peer-a");
        await ExecuteCommandAsync(
            "agent", "mail", "send", "bob", "--subject", "Status", "--body", "All good.", "--no-ping");
        var messageId = await QueryScalarAsync("SELECT id FROM messages WHERE subject = 'Status'");

        // act
        var result = await ExecuteCommandAsync("agent", "ping", "bob");

        // assert
        result.AssertSuccess("claude-code  session-1  claude-peer  ok");

        var call = Assert.Single(peerClient.Calls);
        Assert.Equal("session-1", call.SessionId);
        Assert.Contains(messageId!, call.Message);

        var stored = await QueryScalarAsync(
            "SELECT last_ping_result FROM agent_sessions WHERE session_id = 'session-1'");
        Assert.Equal("ok", stored);
    }

    [Fact]
    public async Task CodexThreadSession_QueuesTheDigestAndRecordsOk()
    {
        // arrange: a live codex-thread session with unread mail waiting -
        // the notifier's required "a codex-thread ping wakes a live thread
        // end to end" CLI-level coverage.
        var queueClient = new FakeCodexQueueClient();
        SetupCodexQueueClient(queueClient);
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");
        await InsertAliveSessionRowAsync(
            FixedHost, "session-1", agentName: "bob", endpointKind: "codex-thread", endpointAddr: "thread-1");
        await ExecuteCommandAsync(
            "agent", "mail", "send", "bob", "--subject", "Status", "--body", "All good.", "--no-ping");
        var messageId = await QueryScalarAsync("SELECT id FROM messages WHERE subject = 'Status'");

        // act
        var result = await ExecuteCommandAsync("agent", "ping", "bob");

        // assert
        result.AssertSuccess("claude-code  session-1  codex-thread  ok");

        var call = Assert.Single(queueClient.Calls);
        Assert.Equal("thread-1", call.ThreadId);
        Assert.Contains(messageId!, call.Message);

        var stored = await QueryScalarAsync(
            "SELECT last_ping_result FROM agent_sessions WHERE session_id = 'session-1'");
        Assert.Equal("ok", stored);
    }

    [Fact]
    public async Task NoEndpointSession_IsSkippedAndNotRecorded()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");
        await InsertAliveSessionRowAsync(FixedHost, "session-1", agentName: "bob");

        // act
        var result = await ExecuteCommandAsync("agent", "ping", "bob");

        // assert
        result.AssertSuccess("claude-code  session-1  none  skipped-no-endpoint");

        var stored = await QueryScalarAsync(
            "SELECT last_ping_result FROM agent_sessions WHERE session_id = 'session-1'");
        Assert.Null(stored);
    }

    [Fact]
    public async Task NoWorkspace_ReturnsError()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "ping", "bob");

        // assert
        result.AssertError(
            """
            No agent workspace found. Run `nitro agent init` first.
            """);
    }

    [Fact]
    public async Task SuccessfulPing_Should_EnforceCooldown_OnAnImmediateSecondAttempt()
    {
        // arrange
        var peerClient = new FakeClaudePeerClient();
        SetupClaudePeerClient(peerClient);
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");
        await InsertAliveSessionRowAsync(
            FixedHost, "session-1", agentName: "bob", endpointKind: "claude-peer", endpointAddr: "peer-a");
        await ExecuteCommandAsync(
            "agent", "mail", "send", "bob", "--subject", "Status", "--body", "All good.", "--no-ping");

        // act: two manual pings at the exact same instant - FakeTime never
        // advances between calls unless a test advances it itself.
        var first = await ExecuteCommandAsync("agent", "ping", "bob");
        var second = await ExecuteCommandAsync("agent", "ping", "bob");

        // assert: the first attempt reaches the transport and its success
        // starts the session gate's cooldown; the second finds the gate
        // busy and never reaches the transport at all.
        first.AssertSuccess("claude-code  session-1  claude-peer  ok");
        second.AssertSuccess("claude-code  session-1  claude-peer  skipped-cooldown");
        Assert.Single(peerClient.Calls);
    }

    [Fact]
    public async Task FailedPing_Should_BeImmediatelyReclaimable_OnAnImmediateSecondAttempt()
    {
        // arrange
        var peerClient = new FakeClaudePeerClient { NextOutcome = ClaudePeerSendOutcome.AccessDenied };
        SetupClaudePeerClient(peerClient);
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");
        await InsertAliveSessionRowAsync(
            FixedHost, "session-1", agentName: "bob", endpointKind: "claude-peer", endpointAddr: "peer-a");
        await ExecuteCommandAsync(
            "agent", "mail", "send", "bob", "--subject", "Status", "--body", "All good.", "--no-ping");

        // act: two manual pings at the exact same instant.
        var first = await ExecuteCommandAsync("agent", "ping", "bob");
        var second = await ExecuteCommandAsync("agent", "ping", "bob");

        // assert: an access-denied (manual failure) attempt never starts a
        // cooldown, so the very next attempt reaches the transport again
        // instead of finding the gate busy.
        first.AssertSuccess("claude-code  session-1  claude-peer  error");
        second.AssertSuccess("claude-code  session-1  claude-peer  error");
        Assert.Equal(2, peerClient.Calls.Count);
    }

    [Fact]
    public async Task ManualPing_Should_SkipTheSession_When_ADaemonShapedOwnerAlreadyHoldsTheGate()
    {
        // arrange: a live claude-peer session, and a concurrent owner
        // (standing in for a foreground-mail dispatch or a future
        // out-of-process daemon) already holding this exact session
        // generation's ping gate.
        var peerClient = new FakeClaudePeerClient();
        SetupClaudePeerClient(peerClient);
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");
        await InsertAliveSessionRowAsync(
            FixedHost, "session-1", agentName: "bob", endpointKind: "claude-peer", endpointAddr: "peer-a");
        await ExecuteCommandAsync(
            "agent", "mail", "send", "bob", "--subject", "Status", "--body", "All good.", "--no-ping");
        await InsertSessionPingGateRowAsync(FixedHost, "session-1", "daemon-attempt-1", FakeTime.GetUtcNow());

        // act
        var result = await ExecuteCommandAsync("agent", "ping", "bob");

        // assert: skipped, no transport call at all, and the concurrent
        // owner's reservation is untouched - exactly one owner can ever
        // hold this session's gate at a time, and it was not this attempt.
        result.AssertSuccess("claude-code  session-1  claude-peer  skipped-cooldown");
        Assert.Empty(peerClient.Calls);

        var gateAttemptId = await QueryScalarAsync(
            "SELECT attempt_id FROM session_ping_gates WHERE session_id = 'session-1'");
        Assert.Equal("daemon-attempt-1", gateAttemptId);

        var pingResult = await QueryScalarAsync(
            "SELECT last_ping_result FROM agent_sessions WHERE session_id = 'session-1'");
        Assert.Null(pingResult);
    }

    /// <summary>
    /// Inserts a <c>session_ping_gates</c> row for the current test
    /// process's own pid and start time (matching <see cref="AgentCommandTestBase.InsertAliveSessionRowAsync"/>'s
    /// generation), simulating a concurrent attempt already holding the
    /// exact session generation's gate.
    /// </summary>
    private async Task InsertSessionPingGateRowAsync(
        string host, string sessionId, string attemptId, DateTimeOffset now)
    {
        using var process = System.Diagnostics.Process.GetCurrentProcess();
        var pid = process.Id;
        var procStart = ProcStat.ReadStartTicks(pid)!;

        await using var connection = new SqliteConnection($"Data Source={DatabasePath};Pooling=False");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO session_ping_gates (
                harness, session_id, host, pid, proc_start, attempt_id, acquired_at, expires_at
            ) VALUES (
                'claude-code', $sessionId, $host, $pid, $procStart, $attemptId, $now, $expiresAt
            );
            """;
        command.Parameters.AddWithValue("$sessionId", sessionId);
        command.Parameters.AddWithValue("$host", host);
        command.Parameters.AddWithValue("$pid", pid);
        command.Parameters.AddWithValue("$procStart", procStart);
        command.Parameters.AddWithValue("$attemptId", attemptId);
        command.Parameters.AddWithValue("$now", now);
        command.Parameters.AddWithValue("$expiresAt", now + TimeSpan.FromSeconds(30));

        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }
}
