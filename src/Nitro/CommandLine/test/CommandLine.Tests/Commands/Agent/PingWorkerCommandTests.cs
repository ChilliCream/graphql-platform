namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Covers <c>nitro agent ping-worker</c>'s CLI surface only. Its actual
/// transport work is covered at the service level by
/// <c>PingSessionExecutorTests</c> (against a fake queue client); running
/// it here would route through the real <c>ICodexQueueClient</c>, and this
/// machine has a real <c>codex</c> binary on PATH.
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
              Internal: performs one already-leased ping attempt. Spawned by the notifier; not for direct use.

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
}
