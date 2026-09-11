using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Never reached by the codex-thread end-to-end smoke test, but required to
/// satisfy <see cref="PingSessionExecutor"/>'s constructor.
/// </summary>
internal sealed class NoopOpencodeServerClient : IOpencodeServerClient
{
    public Task<string> PushMessageAsync(
        string serverUrl, string sessionId, string text, string? secret, CancellationToken cancellationToken)
        => Task.FromResult(AgentPingResult.Ok);

    public Task<string> PingAsync(
        string serverUrl, string sessionId, string? secret, CancellationToken cancellationToken)
        => Task.FromResult(AgentPingResult.Ok);
}
