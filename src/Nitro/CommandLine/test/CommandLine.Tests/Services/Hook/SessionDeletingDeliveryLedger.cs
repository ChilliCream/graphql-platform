using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

internal sealed class SessionDeletingDeliveryLedger(
    ISessionDeliveryLedger inner,
    IAgentSessionRegistry sessionRegistry,
    AgentSessionGeneration generation) : ISessionDeliveryLedger
{
    private bool _deleted;

    public Task<IReadOnlyList<string>> ReserveAsync(
        string harness,
        string sessionId,
        IReadOnlyList<string> messageIds,
        string channel,
        DateTimeOffset deliveredAt,
        CancellationToken cancellationToken)
        => inner.ReserveAsync(harness, sessionId, messageIds, channel, deliveredAt, cancellationToken);

    public async Task<IReadOnlyList<string>> ReserveAsync(
        AgentSessionGeneration reserveGeneration,
        IReadOnlyList<string> messageIds,
        string channel,
        DateTimeOffset deliveredAt,
        CancellationToken cancellationToken)
    {
        if (!_deleted)
        {
            _deleted = true;
            await sessionRegistry.EndAsync(generation, cancellationToken);
        }

        return await inner.ReserveAsync(
            reserveGeneration,
            messageIds,
            channel,
            deliveredAt,
            cancellationToken);
    }

    public Task ReleaseAsync(
        AgentSessionGeneration generation,
        IReadOnlyList<string> messageIds,
        string channel,
        CancellationToken cancellationToken)
        => inner.ReleaseAsync(generation, messageIds, channel, cancellationToken);
}
