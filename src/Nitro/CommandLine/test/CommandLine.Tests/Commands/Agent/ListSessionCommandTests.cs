using System.Diagnostics;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

public sealed class ListSessionCommandTests : AgentCommandTestBase
{
    private const string FixedHost = "host-list-session-tests";

    public ListSessionCommandTests(NitroCommandFixture fixture) : base(fixture)
    {
        SetupInstanceId(FixedHost);
    }

    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "session", "list", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              List live harness sessions in this workspace's agent database.

            Usage:
              nitro agent session list [options]

            Options:
              --output <json>  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help   Show help and usage information

            Example:
              nitro agent session list
            """);
    }

    [Fact]
    public async Task NoSessions_PrintsEmptyMessage()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "session", "list");

        // assert
        result.AssertSuccess(
            """
            No live sessions.
            """);
    }

    [Fact]
    public async Task JsonOutput_NoSessions_ReturnsEmptyArray()
    {
        // arrange
        await InitWorkspaceAsync();
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "session", "list");

        // assert
        result.AssertSuccess(
            """
            {
              "items": []
            }
            """);
    }

    [Fact]
    public async Task UnclaimedAliveSession_IsReportedUnreachable_When_NoEndpointIsRecorded()
    {
        // arrange
        await InitWorkspaceAsync();
        await InsertAliveSessionRowAsync(FixedHost, "session-1", agentName: null, bindingKind: "none");

        // act
        var result = await ExecuteCommandAsync("agent", "session", "list");

        // assert
        var line = Assert.Single(result.StdOut.Split('\n'));
        Assert.Contains("claude-code", line);
        Assert.Contains("session-1", line);
        Assert.Contains("unreachable", line);
        Assert.Contains("unclaimed", line);
    }

    [Fact]
    public async Task ClaimedSession_ShowsAgentNameAndBindingKind()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "pascal");
        await InsertAliveSessionRowAsync(FixedHost, "session-1", agentName: "pascal", bindingKind: "explicit");

        // act
        var result = await ExecuteCommandAsync("agent", "session", "list");

        // assert
        var line = Assert.Single(result.StdOut.Split('\n'));
        Assert.Contains("claimed by pascal (explicit)", line);
    }

    [Fact]
    public async Task DeadCurrentInstanceSession_IsReapedAndOmitted()
    {
        // arrange
        await InitWorkspaceAsync();
        await InsertDeadSessionRowAsync(FixedHost, "session-dead");

        // act
        var result = await ExecuteCommandAsync("agent", "session", "list");

        // assert
        result.AssertSuccess(
            """
            No live sessions.
            """);
    }

    [Fact]
    public async Task RemoteSession_IsReportedRemote_And_NeverReaped()
    {
        // arrange
        await InitWorkspaceAsync();
        await InsertDeadSessionRowAsync("some-other-host", "session-remote");

        // act
        var result = await ExecuteCommandAsync("agent", "session", "list");

        // assert
        var line = Assert.Single(result.StdOut.Split('\n'));
        Assert.Contains("session-remote", line);
        Assert.Contains("remote", line);
    }

    [Fact]
    public async Task UnsupportedEndpoint_IsDistinctFromNoEndpoint()
    {
        // arrange: claude-peer is a real, recorded endpoint the notifier
        // simply has no transport for, distinct diagnostic signal from
        // `unreachable` (endpoint_kind 'none', nothing to attempt at all).
        await InitWorkspaceAsync();
        await InsertUnsupportedPingResultSessionAsync(FixedHost, "session-unsupported");

        // act
        var result = await ExecuteCommandAsync("agent", "session", "list");

        // assert
        var line = Assert.Single(result.StdOut.Split('\n'));
        Assert.Contains("last ping unsupported", line);
    }

    [Fact]
    public async Task UnsupportedEndpoint_SurfacesInJsonOutput()
    {
        // arrange
        await InitWorkspaceAsync();
        await InsertUnsupportedPingResultSessionAsync(FixedHost, "session-unsupported");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "session", "list");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var row = document.RootElement.GetProperty("items").EnumerateArray().Single();
        Assert.Equal("unsupported", row.GetProperty("lastPingResult").GetString());
    }

    [Fact]
    public async Task NoWorkspace_ReturnsError()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "session", "list");

        // assert
        result.AssertError(
            """
            No agent workspace found. Run `nitro agent init` first.
            """);
    }

    private async Task InsertAliveSessionRowAsync(
        string host, string sessionId, string? agentName, string bindingKind)
    {
        using var process = Process.GetCurrentProcess();
        var pid = process.Id;
        var procStart = process.StartTime.ToUniversalTime();

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
    /// Inserts a live, unclaimed <c>claude-peer</c> session and immediately
    /// stamps its <c>last_ping_result</c> - the notifier's own write, done
    /// directly here since building it requires the full Notifier pipeline
    /// this command test does not otherwise exercise.
    /// </summary>
    private async Task InsertUnsupportedPingResultSessionAsync(string host, string sessionId)
    {
        // Explicitly qualified: the local overload above hides every base
        // class overload of this name, including the one that accepts
        // endpointKind/endpointAddr.
        await base.InsertAliveSessionRowAsync(
            host, sessionId, agentName: null, bindingKind: "none",
            endpointKind: "claude-peer", endpointAddr: "peer-a");

        await using var connection = new SqliteConnection($"Data Source={DatabasePath};Pooling=False");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE agent_sessions SET last_ping_result = 'unsupported' WHERE session_id = $sessionId;";
        command.Parameters.AddWithValue("$sessionId", sessionId);
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    private async Task InsertDeadSessionRowAsync(string host, string sessionId)
    {
        await using var connection = new SqliteConnection($"Data Source={DatabasePath};Pooling=False");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO agent_sessions (
                harness, session_id, agent_name, binding_kind, host, pid, proc_start,
                cwd, workspace_path, endpoint_kind, endpoint_addr, started_at, last_beat_at
            ) VALUES (
                'claude-code', $sessionId, NULL, 'none', $host, 999999, $now,
                '/work', '/work/.nitro/agents', 'none', '', $now, $now
            );
            """;
        command.Parameters.AddWithValue("$sessionId", sessionId);
        command.Parameters.AddWithValue("$host", host);
        command.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow);

        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }
}
