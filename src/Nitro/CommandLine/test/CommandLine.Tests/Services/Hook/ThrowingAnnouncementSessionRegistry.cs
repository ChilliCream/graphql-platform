using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Wraps a real <see cref="IAgentSessionRegistry"/>, delegating every member
/// except <see cref="ClaimAnnouncementAsync"/>, which always throws -
/// standing in for a registry failure that lands strictly after a digest
/// reservation has already committed.
/// </summary>
internal sealed class ThrowingAnnouncementSessionRegistry(IAgentSessionRegistry inner) : IAgentSessionRegistry
{
    public Task<AgentSessionRecord> StartAsync(
        AgentSessionGeneration generation,
        string cwd,
        string workspacePath,
        string endpointKind,
        string endpointAddr,
        string? envActor,
        CancellationToken cancellationToken)
        => inner.StartAsync(generation, cwd, workspacePath, endpointKind, endpointAddr, envActor, cancellationToken);

    public Task<AgentSessionClaimResult> ClaimAsync(
        AgentSessionGeneration generation, string actor, bool forceRebind, CancellationToken cancellationToken)
        => inner.ClaimAsync(generation, actor, forceRebind, cancellationToken);

    public Task<bool> EndAsync(AgentSessionGeneration generation, CancellationToken cancellationToken)
        => inner.EndAsync(generation, cancellationToken);

    public Task<AgentSessionRecord?> FindByGenerationAsync(
        AgentSessionGeneration generation, CancellationToken cancellationToken)
        => inner.FindByGenerationAsync(generation, cancellationToken);

    public Task ResetBlockBudgetAsync(AgentSessionGeneration generation, CancellationToken cancellationToken)
        => inner.ResetBlockBudgetAsync(generation, cancellationToken);

    public Task<int?> IncrementBlockBudgetAsync(AgentSessionGeneration generation, CancellationToken cancellationToken)
        => inner.IncrementBlockBudgetAsync(generation, cancellationToken);

    public Task<IReadOnlyList<AgentSessionRecord>> ReapAsync(CancellationToken cancellationToken)
        => inner.ReapAsync(cancellationToken);

    public Task<IReadOnlyList<AgentSessionView>> ListAsync(CancellationToken cancellationToken)
        => inner.ListAsync(cancellationToken);

    public Task<IReadOnlyList<AgentSessionParticipant>> ListParticipantsAsync(CancellationToken cancellationToken)
        => inner.ListParticipantsAsync(cancellationToken);

    public Task<bool> TouchAsync(AgentSessionGeneration generation, CancellationToken cancellationToken)
        => inner.TouchAsync(generation, cancellationToken);

    public Task<bool> RecordHarnessVersionAsync(
        AgentSessionGeneration generation, string harnessVersion, CancellationToken cancellationToken)
        => inner.RecordHarnessVersionAsync(generation, harnessVersion, cancellationToken);

    public Task<bool> SetRoleAsync(AgentSessionGeneration generation, string role, CancellationToken cancellationToken)
        => inner.SetRoleAsync(generation, role, cancellationToken);

    public Task<AgentSessionRegisterResult> RegisterAsync(
        AgentSessionGeneration generation,
        string actor,
        string role,
        string client,
        bool forceRebind,
        CancellationToken cancellationToken)
        => inner.RegisterAsync(generation, actor, role, client, forceRebind, cancellationToken);

    public Task<AgentSessionRecord?> FindBySessionIdAsync(
        string harness, string host, string sessionId, CancellationToken cancellationToken)
        => inner.FindBySessionIdAsync(harness, host, sessionId, cancellationToken);

    public Task<IReadOnlyList<AgentSessionRecord>> FindLiveClaimedByAgentNameAsync(
        string agentName, CancellationToken cancellationToken)
        => inner.FindLiveClaimedByAgentNameAsync(agentName, cancellationToken);

    public Task<bool> TryClaimPingCooldownAsync(
        AgentSessionRecord session,
        string attemptId,
        DateTimeOffset now,
        TimeSpan cooldown,
        CancellationToken cancellationToken)
        => inner.TryClaimPingCooldownAsync(session, attemptId, now, cooldown, cancellationToken);

    public Task WritePingResultAsync(
        string harness,
        string sessionId,
        string attemptId,
        string result,
        string? detail,
        CancellationToken cancellationToken)
        => inner.WritePingResultAsync(harness, sessionId, attemptId, result, detail, cancellationToken);

    public Task ArmAnnouncementAsync(AgentSessionGeneration generation, CancellationToken cancellationToken)
        => inner.ArmAnnouncementAsync(generation, cancellationToken);

    public Task<bool> ClaimAnnouncementAsync(AgentSessionGeneration generation, CancellationToken cancellationToken)
        => throw new InvalidOperationException("Simulated announcement-claim failure.");

    public Task RearmIdlePushAsync(AgentSessionGeneration generation, CancellationToken cancellationToken)
        => inner.RearmIdlePushAsync(generation, cancellationToken);

    public Task<bool> ClaimIdlePushAsync(AgentSessionGeneration generation, CancellationToken cancellationToken)
        => inner.ClaimIdlePushAsync(generation, cancellationToken);
}
