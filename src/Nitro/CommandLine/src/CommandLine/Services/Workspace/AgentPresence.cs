namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// The combined presence of an actor's surviving sessions.
/// </summary>
internal sealed record AgentPresence(
    string State,
    bool Conflicted,
    int SessionCount,
    string? EndpointKind,
    string? EndpointAddr,
    string? Activity)
{
    /// <summary>
    /// The display order for distinct session states in a conflicted presence.
    /// </summary>
    private static readonly string[] s_statePriority =
    [
        AgentSessionState.Online,
        AgentSessionState.Unreachable,
        AgentSessionState.Unobservable,
        AgentSessionState.Remote
    ];

    /// <summary>
    /// The presence of an agent with no live sessions at all.
    /// </summary>
    public static readonly AgentPresence Offline = new(AgentPresenceState.Offline, false, 0, null, null, null);

    /// <summary>
    /// Combines one actor's sessions into a presence, joining distinct states in priority
    /// order and marking conflicts when states differ. Endpoint details require exactly
    /// one session; activity additionally requires that session to be online in Claude Code.
    /// </summary>
    public static AgentPresence Compute(
        IReadOnlyList<AgentSessionView> sessions, IClaudeSessionActivityReader activityReader)
    {
        if (sessions.Count == 0)
        {
            return Offline;
        }

        var distinctStates = sessions.Select(v => v.State).Distinct().ToArray();
        var conflicted = distinctStates.Length > 1;
        var state = conflicted
            ? string.Join("+", s_statePriority.Where(distinctStates.Contains))
            : distinctStates[0];

        string? endpointKind = null;
        string? endpointAddr = null;
        string? activity = null;

        if (sessions.Count == 1)
        {
            var only = sessions[0];
            endpointKind = only.Session.EndpointKind;
            endpointAddr = only.Session.EndpointAddr;

            if (only.State == AgentSessionState.Online && only.Session.Harness == AgentSessionHarness.ClaudeCode)
            {
                activity = activityReader.GetStatus(only.Session.SessionId);
            }
        }

        return new AgentPresence(state, conflicted, sessions.Count, endpointKind, endpointAddr, activity);
    }
}
