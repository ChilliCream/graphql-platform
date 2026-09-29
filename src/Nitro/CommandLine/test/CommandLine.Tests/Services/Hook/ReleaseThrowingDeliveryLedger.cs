using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Delegates <see cref="FindDeliveredAsync"/> and <see cref="ReserveAsync"/> to
/// <see cref="IAgentDeliveryLedger"/>. <see cref="ReleaseAsync"/> throws <see cref="NotSupportedException"/>.
/// </summary>
internal sealed class ReleaseThrowingDeliveryLedger(IAgentDeliveryLedger inner) : IAgentDeliveryLedger
{
    public Task<IReadOnlyList<string>> FindDeliveredAsync(
        string agent, IReadOnlyList<string> messageIds, CancellationToken cancellationToken)
        => inner.FindDeliveredAsync(agent, messageIds, cancellationToken);

    public Task<IReadOnlyList<string>> ReserveAsync(
        string agent,
        IReadOnlyList<string> messageIds,
        string channel,
        DateTimeOffset deliveredAt,
        CancellationToken cancellationToken)
        => inner.ReserveAsync(agent, messageIds, channel, deliveredAt, cancellationToken);

    public Task ReleaseAsync(
        string agent, IReadOnlyList<string> messageIds, string channel, CancellationToken cancellationToken)
        => throw new NotSupportedException("Simulated compensating-release failure.");
}
