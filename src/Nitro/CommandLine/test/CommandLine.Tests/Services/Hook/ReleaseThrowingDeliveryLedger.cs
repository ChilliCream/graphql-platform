using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Delegates both reservation overloads to <see cref="ISessionDeliveryLedger"/>.
/// <see cref="ReleaseAsync"/> throws <see cref="NotSupportedException"/>.
/// </summary>
internal sealed class ReleaseThrowingDeliveryLedger(ISessionDeliveryLedger inner) : ISessionDeliveryLedger
{
    public Task<IReadOnlyList<string>> ReserveAsync(
        string harness,
        string sessionId,
        IReadOnlyList<string> messageIds,
        string channel,
        DateTimeOffset deliveredAt,
        CancellationToken cancellationToken)
        => inner.ReserveAsync(harness, sessionId, messageIds, channel, deliveredAt, cancellationToken);

    public Task<IReadOnlyList<string>> ReserveAsync(
        AgentSessionGeneration generation,
        IReadOnlyList<string> messageIds,
        string channel,
        DateTimeOffset deliveredAt,
        CancellationToken cancellationToken)
        => inner.ReserveAsync(generation, messageIds, channel, deliveredAt, cancellationToken);

    public Task ReleaseAsync(
        AgentSessionGeneration generation,
        IReadOnlyList<string> messageIds,
        string channel,
        CancellationToken cancellationToken)
        => throw new NotSupportedException("Simulated compensating-release failure.");
}
