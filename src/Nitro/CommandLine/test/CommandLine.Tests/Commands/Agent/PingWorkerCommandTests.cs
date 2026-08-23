namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Covers <c>nitro agent ping-worker</c>'s CLI surface only. Its actual
/// codex-thread work is covered at the service level by
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
              Internal: performs one already-leased codex-thread ping attempt. Spawned by the notifier; not for direct use.

            Usage:
              nitro agent ping-worker [options]

            Options:
              --harness <harness> (REQUIRED)              Internal: the target session's harness. Set by the notifier, not for direct use.
              --session-id <session-id> (REQUIRED)        Internal: the target session's id. Set by the notifier, not for direct use.
              --actor <actor> (REQUIRED)                  Internal: the bound actor whose unread mail to digest. Set by the notifier, not for direct use.
              --endpoint-addr <endpoint-addr> (REQUIRED)  Internal: the target endpoint address (a Codex thread id). Set by the notifier, not for direct use.
              --attempt <attempt> (REQUIRED)              Internal: the attempt id this worker's result write is conditioned on. Set by the notifier, not for direct use.
              --slot <slot> (REQUIRED)                    Internal: the ping_leases slot already acquired for this attempt. Set by the notifier, not for direct use.
              -?, -h, --help                              Show help and usage information
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
