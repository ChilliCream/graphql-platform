using ChilliCream.Nitro.CommandLine.Services.Notify;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Covers <c>nitro agent ping-worker</c>'s CLI surface only. Its actual
/// transport work is covered at the service level by
/// <c>PingSessionExecutorTests</c> (against a fake queue client); running
/// it here would route through the real <c>ICodexQueueClient</c>, and this
/// machine has a real <c>codex</c> binary on PATH. The notifier no longer
/// spawns this command for automatic mail wake (see <c>NotifierTests</c>
/// and <c>ActorWakeDispatcherTests</c>, which dispatch every target
/// in-process): it is retained only as the explicit, non-mail compatibility
/// path <c>IPingWorkerLauncher</c> still exists for, which
/// <c>SendMailCommandTests.NoPing_Should_SkipTheNotifier_And_NeverInvokeTheLauncher</c>
/// already asserts the automatic-mail path never reaches.
/// </summary>
public sealed class PingWorkerCommandTests : AgentCommandTestBase
{
    public PingWorkerCommandTests(NitroCommandFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "ping-worker", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Internal: performs one already-leased ping attempt. Not spawned by the notifier; kept only for an explicit, non-mail out-of-process caller.

            Usage:
              nitro agent ping-worker [options]

            Options:
              --harness <harness> (REQUIRED)                         Internal: the target session's harness. Set by the notifier, not for direct use.
              --session-id <session-id> (REQUIRED)                   Internal: the target session's id. Set by the notifier, not for direct use.
              --actor <actor> (REQUIRED)                             Internal: the bound actor whose unread mail to digest. Set by the notifier, not for direct use.
              --endpoint-kind <claude-peer|codex-thread> (REQUIRED)  Internal: the target endpoint kind. Set by the notifier, not for direct use.
              --endpoint-addr <endpoint-addr> (REQUIRED)             Internal: the target endpoint address. Set by the notifier, not for direct use.
              --pid <pid> (REQUIRED)                                 Internal: the target session process id. Set by the notifier, not for direct use.
              --attempt <attempt> (REQUIRED)                         Internal: the attempt id this worker's result write is conditioned on. Set by the notifier, not for direct use.
              --slot <slot> (REQUIRED)                               Internal: the ping_leases slot already acquired for this attempt. Set by the notifier, not for direct use.
              --deadline <deadline> (REQUIRED)                       Internal: the absolute UTC deadline this attempt's digest and transport work must finish before, fixed when the notifier acquired the lease. Set by the notifier, not for direct use.
              -?, -h, --help                                         Show help and usage information
            """);
    }

    [Fact]
    public async Task MissingRequiredOptions_ReturnsParseError()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "ping-worker");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("--harness", result.StdErr);
    }

    [Fact]
    public async Task StaleAttempt_Should_AffectZeroRows_When_ANewerPingAlreadyStampedTheRow()
    {
        // arrange: a live claude-peer session, already pinged once
        // successfully through `agent ping` - the row's last_ping_attempt
        // now holds that real attempt's id.
        const string fixedHost = "host-ping-worker-tests";
        var peerClient = new FakeClaudePeerClient();
        SetupClaudePeerClient(peerClient);
        SetupInstanceId(fixedHost);
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register", "--actor", "bob");
        await InsertAliveSessionRowAsync(
            fixedHost, "session-1", agentName: "bob", endpointKind: "claude-peer", endpointAddr: "peer-a");
        await ExecuteCommandAsync(
            "agent", "mail", "send", "bob", "--subject", "Status", "--body", "All good.", "--no-ping");

        var pingResult = await ExecuteCommandAsync("agent", "ping", "bob");
        pingResult.AssertSuccess("claude-code  session-1  claude-peer  ok");

        using var process = System.Diagnostics.Process.GetCurrentProcess();
        var pid = process.Id;

        // act: a stale worker - an attempt id the row never stamped,
        // standing in for a completion that only arrives after a newer
        // attempt already took over the session - still runs its own
        // transport call.
        peerClient.NextOutcome = ClaudePeerSendOutcome.AccessDenied;
        var workerResult = await ExecuteCommandAsync(
            "agent", "ping-worker",
            "--harness", "claude-code",
            "--session-id", "session-1",
            "--actor", "bob",
            "--endpoint-kind", "claude-peer",
            "--endpoint-addr", "peer-a",
            "--pid", pid.ToString(),
            "--attempt", "stale-attempt-id",
            "--slot", "1",
            "--deadline", "2026-01-01T00:00:20Z");

        // assert: the stale worker's own transport call happened...
        Assert.Equal(0, workerResult.ExitCode);
        Assert.Equal(2, peerClient.Calls.Count);

        // ...but its result write is fenced out by last_ping_attempt no
        // longer matching its (stale) attempt id, so the row still shows
        // the real ping's outcome, not the stale worker's access-denied one.
        var stored = await QueryScalarAsync(
            "SELECT last_ping_result FROM agent_sessions WHERE session_id = 'session-1'");
        Assert.Equal("ok", stored);
    }
}
