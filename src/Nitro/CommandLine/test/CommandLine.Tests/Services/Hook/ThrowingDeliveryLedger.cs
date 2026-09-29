using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// An <see cref="IAgentDeliveryLedger"/> whose every member throws, used
/// to stand in for a mail-store or ledger failure without touching a real
/// workspace database.
/// </summary>
internal sealed class ThrowingDeliveryLedger : IAgentDeliveryLedger
{
    public Task<IReadOnlyList<string>> FindDeliveredAsync(
        string agent, IReadOnlyList<string> messageIds, CancellationToken cancellationToken)
        => throw new InvalidOperationException("Simulated delivery-ledger failure.");

    public Task<IReadOnlyList<string>> ReserveAsync(
        string agent,
        IReadOnlyList<string> messageIds,
        string channel,
        DateTimeOffset deliveredAt,
        CancellationToken cancellationToken)
        => throw new InvalidOperationException("Simulated delivery-ledger failure.");

    public Task ReleaseAsync(
        string agent, IReadOnlyList<string> messageIds, string channel, CancellationToken cancellationToken)
        => throw new InvalidOperationException("Simulated delivery-ledger failure.");
}
