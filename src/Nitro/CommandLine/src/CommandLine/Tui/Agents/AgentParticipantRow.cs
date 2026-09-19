using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tui.Agents;

/// <summary>
/// A live participant and its optional Claude activity.
/// <see cref="AgentsState"/> supplies activity only for online Claude Code sessions.
/// </summary>
internal sealed record AgentParticipantRow(AgentSessionParticipant Participant, string? Activity)
{
    /// <summary>
    /// The label shown in place of an actor name for a session with no bound identity.
    /// </summary>
    public const string UnboundLabel = "(unbound)";

    /// <summary>
    /// The key <see cref="AgentsState"/> selects and diffs this row by.
    /// </summary>
    public AgentSessionKey Key => AgentSessionKey.From(Participant.Session);
}
