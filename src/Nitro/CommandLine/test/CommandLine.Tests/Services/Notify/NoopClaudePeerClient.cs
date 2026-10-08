using ChilliCream.Nitro.CommandLine.Services.Notify;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// No-op <see cref="IClaudePeerClient"/> that always returns <see cref="ClaudePeerSendOutcome.Ok"/>.
/// </summary>
internal sealed class NoopClaudePeerClient : IClaudePeerClient
{
    public Task<ClaudePeerSendOutcome> SendAsync(
        string sessionId, string message, CancellationToken cancellationToken)
        => Task.FromResult(ClaudePeerSendOutcome.Ok);
}
