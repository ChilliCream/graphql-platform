using ChilliCream.Nitro.CommandLine.Services.Notify;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Never reached by the codex-thread end-to-end smoke test, but required to
/// satisfy <see cref="PingSessionExecutor"/>'s constructor.
/// </summary>
internal sealed class NoopClaudePeerClient : IClaudePeerClient
{
    public Task<ClaudePeerSendOutcome> SendAsync(
        string sessionId, string message, CancellationToken cancellationToken)
        => Task.FromResult(ClaudePeerSendOutcome.Ok);
}
