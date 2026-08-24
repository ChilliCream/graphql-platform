using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Agents;

/// <summary>
/// Builds <see cref="AgentSessionRecord"/> and <see cref="AgentSessionParticipant"/>
/// instances with sensible defaults for agents mode and agent detail model
/// tests.
/// </summary>
internal static class AgentSessionParticipantBuilder
{
    private static readonly DateTimeOffset DefaultNow = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static AgentSessionRecord Session(
        string sessionId = "s-1",
        string harness = "claude-code",
        string? agentName = null,
        string role = "",
        string harnessVersion = "",
        string host = "host-a",
        int pid = 4242,
        string cwd = "/work",
        string workspacePath = "/work/.nitro/agents",
        string endpointKind = "claude-peer",
        string endpointAddr = "peer",
        DateTimeOffset? startedAt = null,
        DateTimeOffset? lastBeatAt = null)
        => new()
        {
            Harness = harness,
            SessionId = sessionId,
            AgentName = agentName,
            BindingKind = agentName is null ? AgentSessionBindingKind.None : AgentSessionBindingKind.Explicit,
            Host = host,
            Pid = pid,
            ProcStart = "1000000",
            Cwd = cwd,
            WorkspacePath = workspacePath,
            EndpointKind = endpointKind,
            EndpointAddr = endpointAddr,
            StartedAt = startedAt ?? DefaultNow,
            LastBeatAt = lastBeatAt ?? startedAt ?? DefaultNow,
            BlockBudgetUsed = 0,
            Role = role,
            HarnessVersion = harnessVersion,
            ProcessScope = "",
            ProcStartLegacy = false
        };

    /// <summary>
    /// Builds a participant with <paramref name="state"/> supplied directly
    /// (as <see cref="FakeAgentSessionRegistry"/> hands it straight back,
    /// rather than recomputing it from host/pid the way the real registry
    /// does - that computation is covered separately by
    /// <c>AgentSessionRegistryTests</c>). An unreachable state clears the
    /// endpoint columns, mirroring how the real registry only reports
    /// "online" when an endpoint is registered.
    /// </summary>
    public static AgentSessionParticipant Participant(
        string sessionId = "s-1",
        string? agentName = null,
        string role = "",
        string harness = "claude-code",
        string harnessVersion = "",
        string state = AgentSessionState.Online,
        int pid = 4242,
        AgentRecord? agent = null,
        DateTimeOffset? startedAt = null,
        DateTimeOffset? lastBeatAt = null)
    {
        var unreachable = state == AgentSessionState.Unreachable;

        var session = Session(
            sessionId: sessionId,
            harness: harness,
            agentName: agentName,
            role: role,
            harnessVersion: harnessVersion,
            pid: pid,
            endpointKind: unreachable ? AgentSessionEndpointKind.None : AgentSessionEndpointKind.ClaudePeer,
            endpointAddr: unreachable ? "" : "peer",
            startedAt: startedAt,
            lastBeatAt: lastBeatAt);

        return new AgentSessionParticipant(session, agent, state);
    }
}
