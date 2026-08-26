using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tui.Agents;

/// <summary>
/// The live state of the agents list: every live participant from
/// <see cref="IAgentSessionRegistry.ListParticipantsAsync"/>, in the order
/// the registry returns them, which row is selected, and which of the tab's
/// two panes currently holds focus.
/// </summary>
internal sealed class AgentsState(IAgentSessionRegistry sessionRegistry, IClaudeSessionActivityReader activityReader)
{
    /// <summary>
    /// The participant rows currently loaded, ordered by harness then
    /// session id (the registry's own order). One row per live harness
    /// session, including unbound ones; a session that ends or is reaped is
    /// simply absent from the next <see cref="RefreshAsync"/>.
    /// </summary>
    public IReadOnlyList<AgentParticipantRow> Rows { get; private set; } = [];

    /// <summary>
    /// The index of the selected row within <see cref="Rows"/>.
    /// </summary>
    public int SelectedRow { get; set; }

    /// <summary>
    /// Which pane currently holds focus.
    /// </summary>
    public AgentsFocus Focus { get; set; } = AgentsFocus.List;

    /// <summary>
    /// The row at <see cref="SelectedRow"/>, or null when the list is empty
    /// or the row is out of range.
    /// </summary>
    public AgentParticipantRow? SelectedParticipant
        => SelectedRow >= 0 && SelectedRow < Rows.Count ? Rows[SelectedRow] : null;

    /// <summary>
    /// Reloads every live participant from the registry. The selected row
    /// stays selected by its <see cref="AgentSessionKey"/> (harness plus
    /// session id), never by actor name, so two sessions sharing one actor
    /// and a bound session's role being promoted both leave the selection
    /// untouched. Otherwise the selected row is clamped to the new list's
    /// bounds.
    /// </summary>
    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        var selectedKey = SelectedParticipant?.Key;

        var participants = await sessionRegistry.ListParticipantsAsync(cancellationToken);
        Rows = participants.Select(ToRow).ToList();

        var preservedIndex = selectedKey is { } key ? IndexOf(Rows, key) : -1;

        SelectedRow = preservedIndex >= 0
            ? preservedIndex
            : Math.Clamp(SelectedRow, 0, Math.Max(0, Rows.Count - 1));
    }

    /// <summary>
    /// Reads the Claude activity read-through for a single online
    /// claude-code session; every other row carries no activity, since the
    /// read-through only means anything against a session a caller could
    /// plausibly find a live status file for.
    /// </summary>
    private AgentParticipantRow ToRow(AgentSessionParticipant participant)
    {
        var activity = participant.State == AgentSessionState.Online
            && participant.Session.Harness == AgentSessionHarness.ClaudeCode
                ? activityReader.GetStatus(participant.Session.SessionId)
                : null;

        return new AgentParticipantRow(participant, activity);
    }

    private static int IndexOf(IReadOnlyList<AgentParticipantRow> rows, AgentSessionKey key)
    {
        for (var i = 0; i < rows.Count; i++)
        {
            if (rows[i].Key == key)
            {
                return i;
            }
        }

        return -1;
    }
}
