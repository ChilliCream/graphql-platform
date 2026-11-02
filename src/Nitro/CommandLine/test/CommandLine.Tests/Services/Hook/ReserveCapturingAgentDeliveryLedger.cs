using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Wraps a real <see cref="IAgentDeliveryLedger"/>, delegating every call while
/// capturing the <c>messageIds</c> argument of the most recent <c>ReserveAsync</c> call.
/// </summary>
internal sealed class ReserveCapturingAgentDeliveryLedger(IAgentDeliveryLedger inner) : IAgentDeliveryLedger
{
    public IReadOnlyList<string>? LastMessageIds { get; private set; }

    public Task<IReadOnlyList<string>> FindDeliveredAsync(
        string agent, IReadOnlyList<string> messageIds, CancellationToken cancellationToken)
        => inner.FindDeliveredAsync(agent, messageIds, cancellationToken);

    public Task<IReadOnlyList<string>> ReserveAsync(
        string agent,
        IReadOnlyList<string> messageIds,
        string channel,
        DateTimeOffset deliveredAt,
        CancellationToken cancellationToken)
    {
        LastMessageIds = messageIds;
        return inner.ReserveAsync(agent, messageIds, channel, deliveredAt, cancellationToken);
    }

    public Task ReleaseAsync(
        string agent, IReadOnlyList<string> messageIds, string channel, CancellationToken cancellationToken)
        => inner.ReleaseAsync(agent, messageIds, channel, cancellationToken);
}
