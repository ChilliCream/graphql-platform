using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Agents;

/// <summary>
/// An in-memory <see cref="IAgentSessionRegistry"/> exercising the surface
/// <see cref="ChilliCream.Nitro.CommandLine.Tui.Agents.AgentsState"/> consumes
/// (<see cref="ListAsync"/>). Every other member throws
/// <see cref="NotSupportedException"/>.
/// </summary>
internal sealed class FakeAgentSessionRegistry : IAgentSessionRegistry
{
    public List<AgentSessionView> Sessions { get; } = [];

    public Task<IReadOnlyList<AgentSessionView>> ListAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<AgentSessionView>>(Sessions);

    public Task<AgentSessionRecord> StartAsync(
        AgentSessionGeneration generation,
        string cwd,
        string workspacePath,
        string endpointKind,
        string endpointAddr,
        string? envActor,
        CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<AgentSessionClaimResult> ClaimAsync(
        AgentSessionGeneration generation, string actor, bool forceRebind, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<AgentSessionClaimResult> SelfClaimAsync(
        string actor, bool forceRebind, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<bool> EndAsync(AgentSessionGeneration generation, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<AgentSessionRecord>> ReapAsync(CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<bool> TouchAsync(AgentSessionGeneration generation, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<bool> RecordHarnessVersionAsync(
        AgentSessionGeneration generation, string harnessVersion, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<AgentSessionRecord?> FindByGenerationAsync(
        AgentSessionGeneration generation, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task ResetBlockBudgetAsync(AgentSessionGeneration generation, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<int?> IncrementBlockBudgetAsync(AgentSessionGeneration generation, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<AgentSessionRecord>> FindLiveClaimedByAgentNameAsync(
        string agentName, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<bool> TryClaimPingCooldownAsync(
        AgentSessionRecord session,
        string attemptId,
        DateTimeOffset now,
        TimeSpan cooldown,
        CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task WritePingResultAsync(
        string harness,
        string sessionId,
        string attemptId,
        string result,
        string? detail,
        CancellationToken cancellationToken)
        => throw new NotSupportedException();
}
