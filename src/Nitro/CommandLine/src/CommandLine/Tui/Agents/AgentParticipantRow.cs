using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tui.Agents;

/// <summary>
/// One live participant row as the Agents tab renders it: the
/// <see cref="AgentSessionParticipant"/> paired with its Claude activity
/// read-through, only ever set for an online claude-code session (see
/// <see cref="AgentsState.RefreshAsync"/>).
/// </summary>
internal sealed record AgentParticipantRow(AgentSessionParticipant Participant, string? Activity)
{
    /// <summary>
    /// The label shown in place of an actor name for a session with no
    /// bound identity, shared by the list row and the detail pane's Session
    /// section.
    /// </summary>
    public const string UnboundLabel = "(unbound)";

    /// <summary>
    /// The key <see cref="AgentsState"/> selects and diffs this row by.
    /// </summary>
    public AgentSessionKey Key => AgentSessionKey.From(Participant.Session);
}
