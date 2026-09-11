using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// An <see cref="ISessionDeliveryLedger"/> whose every member throws, used
/// to stand in for a mail-store or ledger failure without touching a real
/// workspace database.
/// </summary>
internal sealed class ThrowingDeliveryLedger : ISessionDeliveryLedger
{
    public Task<IReadOnlyList<string>> ReserveAsync(
        string harness,
        string sessionId,
        IReadOnlyList<string> messageIds,
        string channel,
        DateTimeOffset deliveredAt,
        CancellationToken cancellationToken)
        => throw new InvalidOperationException("Simulated delivery-ledger failure.");

    public Task<IReadOnlyList<string>> ReserveAsync(
        AgentSessionGeneration generation,
        IReadOnlyList<string> messageIds,
        string channel,
        DateTimeOffset deliveredAt,
        CancellationToken cancellationToken)
        => throw new InvalidOperationException("Simulated delivery-ledger failure.");

    public Task ReleaseAsync(
        AgentSessionGeneration generation,
        IReadOnlyList<string> messageIds,
        string channel,
        CancellationToken cancellationToken)
        => throw new InvalidOperationException("Simulated delivery-ledger failure.");
}
