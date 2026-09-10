using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Agents;

/// <summary>
/// An in-memory <see cref="IAgentSessionRegistry"/> exercising the surface
/// <see cref="CommandLine.Tui.Agents.AgentsState"/> consumes
/// (<see cref="ListParticipantsAsync"/>) and <see cref="FindLiveClaimedByAgentNameAsync"/>,
/// configurable via <see cref="LiveSessionsByAgentName"/>. Every other member
/// throws <see cref="NotSupportedException"/>.
/// </summary>
internal sealed class FakeAgentSessionRegistry : IAgentSessionRegistry
{
    public List<AgentSessionParticipant> Participants { get; } = [];

    /// <summary>
    /// The rows <see cref="FindLiveClaimedByAgentNameAsync"/> returns, keyed
    /// by agent name; an agent name with no entry gets an empty list.
    /// </summary>
    public Dictionary<string, IReadOnlyList<AgentSessionRecord>> LiveSessionsByAgentName { get; } =
        new(StringComparer.Ordinal);

    public Task<IReadOnlyList<AgentSessionParticipant>> ListParticipantsAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<AgentSessionParticipant>>(Participants);

    public Task<IReadOnlyList<AgentSessionRecord>> FindLiveClaimedByAgentNameAsync(
        string agentName, CancellationToken cancellationToken)
        => Task.FromResult(LiveSessionsByAgentName.TryGetValue(agentName, out var sessions)
            ? sessions
            : (IReadOnlyList<AgentSessionRecord>)[]);

    public Task<IReadOnlyList<AgentSessionView>> ListAsync(CancellationToken cancellationToken)
        => throw new NotSupportedException();

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

    public Task<bool> EndAsync(AgentSessionGeneration generation, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<AgentSessionRecord>> ReapAsync(CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<bool> TouchAsync(AgentSessionGeneration generation, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<bool> RecordHarnessVersionAsync(
        AgentSessionGeneration generation, string harnessVersion, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<bool> SetRoleAsync(AgentSessionGeneration generation, string role, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<AgentSessionRegisterResult> RegisterAsync(
        AgentSessionGeneration generation,
        string actor,
        string role,
        string client,
        bool forceRebind,
        CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<AgentSessionRecord?> FindBySessionIdAsync(
        string harness, string host, string sessionId, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<AgentSessionRecord?> FindByGenerationAsync(
        AgentSessionGeneration generation, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task ResetBlockBudgetAsync(AgentSessionGeneration generation, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<int?> IncrementBlockBudgetAsync(AgentSessionGeneration generation, CancellationToken cancellationToken)
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
