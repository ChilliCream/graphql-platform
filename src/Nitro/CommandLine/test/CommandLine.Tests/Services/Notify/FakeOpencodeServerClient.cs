using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Scriptable <see cref="IOpencodeServerClient"/>: records every push and
/// ping call and returns <see cref="AgentPingResult.Ok"/> from both by
/// default, or the scripted <see cref="NextPushResult"/> /
/// <see cref="NextPingResult"/> otherwise.
/// </summary>
internal sealed class FakeOpencodeServerClient : IOpencodeServerClient
{
    public List<FakeOpencodePushCall> PushCalls { get; } = [];

    public List<FakeOpencodePingCall> PingCalls { get; } = [];

    public string NextPushResult { get; set; } = AgentPingResult.Ok;

    public string NextPingResult { get; set; } = AgentPingResult.Ok;

    public Task<string> PushMessageAsync(
        string serverUrl, string sessionId, string text, string? secret, CancellationToken cancellationToken)
    {
        PushCalls.Add(new FakeOpencodePushCall(serverUrl, sessionId, text, secret));
        return Task.FromResult(NextPushResult);
    }

    public Task<string> PingAsync(
        string serverUrl, string sessionId, string? secret, CancellationToken cancellationToken)
    {
        PingCalls.Add(new FakeOpencodePingCall(serverUrl, sessionId, secret));
        return Task.FromResult(NextPingResult);
    }
}
