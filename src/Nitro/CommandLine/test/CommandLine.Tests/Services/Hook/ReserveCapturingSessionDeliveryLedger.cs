using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Wraps a real <see cref="ISessionDeliveryLedger"/>, delegating every call
/// while capturing the <c>messageIds</c> argument of the most recent
/// <see cref="ReserveAsync"/> call.
/// </summary>
internal sealed class ReserveCapturingSessionDeliveryLedger(ISessionDeliveryLedger inner) : ISessionDeliveryLedger
{
    public IReadOnlyList<string>? LastMessageIds { get; private set; }

    public Task<IReadOnlyList<string>> ReserveAsync(
        string harness,
        string sessionId,
        IReadOnlyList<string> messageIds,
        string channel,
        DateTimeOffset deliveredAt,
        CancellationToken cancellationToken)
    {
        LastMessageIds = messageIds;
        return inner.ReserveAsync(harness, sessionId, messageIds, channel, deliveredAt, cancellationToken);
    }

    public Task<IReadOnlyList<string>> ReserveAsync(
        AgentSessionGeneration generation,
        IReadOnlyList<string> messageIds,
        string channel,
        DateTimeOffset deliveredAt,
        CancellationToken cancellationToken)
    {
        LastMessageIds = messageIds;
        return inner.ReserveAsync(generation, messageIds, channel, deliveredAt, cancellationToken);
    }
}
