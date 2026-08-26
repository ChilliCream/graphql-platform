namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// One agent identity's presence, aggregated across zero or more live
/// <see cref="AgentSessionRecord"/> rows bound to it. Presence has its own
/// minutes-scale lifetime, distinct from the 30-day staleness
/// <see cref="AgentRecord"/> identity carries (<c>agent list --stale</c>).
/// Consumed by both <c>agent list</c> and the TUI Agents tab so the two
/// surfaces agree on what "online" means.
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
    /// The state priority a conflicted agent's states are joined in: the
    /// most-actionable state first (see <see cref="Compute"/>).
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
    /// Aggregates <paramref name="sessions"/> - already filtered to the one
    /// agent's rows - into a single display presence.
    /// <para/>
    /// A same-actor restart can leave more than one live session. When those
    /// sessions disagree on <see cref="AgentSessionView.State"/>, every
    /// distinct state is joined (in <see cref="s_statePriority"/> order)
    /// rather than one being silently picked, and <see cref="Conflicted"/> is
    /// set so a caller can flag it: the plan's "same-actor multi-session
    /// conflicts surfaced, not hidden".
    /// <para/>
    /// The endpoint columns and the Claude activity read-through (via
    /// <paramref name="activityReader"/>, never stored - see
    /// <see cref="IClaudeSessionActivityReader"/>) are only reported when
    /// exactly one session is live: with more than one, which session's
    /// endpoint or activity to show is itself ambiguous, and guessing would
    /// re-hide the exact conflict this method exists to surface.
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
