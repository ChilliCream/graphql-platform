using ChilliCream.Nitro.CommandLine.Services.Notify;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

internal sealed class FakeClaudePeerClient : IClaudePeerClient
{
    public List<FakeClaudePeerCall> Calls { get; } = [];

    public ClaudePeerSendOutcome NextOutcome { get; set; } = ClaudePeerSendOutcome.Ok;

    public Task<ClaudePeerSendOutcome> SendAsync(
        string sessionId,
        string message,
        CancellationToken cancellationToken)
    {
        Calls.Add(new FakeClaudePeerCall(sessionId, message));
        return Task.FromResult(NextOutcome);
    }
}
