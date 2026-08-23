using ChilliCream.Nitro.CommandLine.Tests.Hook;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Runs <c>nitro agent ping</c> against a real workspace database. The
/// codex-thread transport call goes through a fake <c>ICodexQueueClient</c>
/// substituted via <c>SetupCodexQueueClient</c>, so no scenario here ever
/// shells out to a real <c>codex</c> binary.
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
    public async Task ClaudePeerSession_RecordsUnsupported()
    {
        // arrange: a real, recorded endpoint the notifier has no transport
        // for - resolved and recorded entirely in-process, no transport
        // call, so this is safe to run through the full CLI pipeline.
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");
        await InsertAliveSessionRowAsync(
            FixedHost, "session-1", agentName: "bob", endpointKind: "claude-peer", endpointAddr: "peer-a");

        // act
        var result = await ExecuteCommandAsync("agent", "ping", "bob");

        // assert
        result.AssertSuccess("claude-code  session-1  claude-peer  unsupported");

        var stored = await QueryScalarAsync(
            "SELECT last_ping_result FROM agent_sessions WHERE session_id = 'session-1'");
        Assert.Equal("unsupported", stored);
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
}
