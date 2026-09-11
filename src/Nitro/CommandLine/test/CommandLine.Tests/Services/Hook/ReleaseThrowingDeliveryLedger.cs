using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Wraps a real <see cref="ISessionDeliveryLedger"/>, delegating
/// <see cref="ReserveAsync(AgentSessionGeneration, IReadOnlyList{string}, string, DateTimeOffset, CancellationToken)"/>
/// while <see cref="ReleaseAsync"/> always throws - standing in for a
/// compensating release that itself fails, so the primary exception it was
/// meant to accompany can be told apart from it.
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
