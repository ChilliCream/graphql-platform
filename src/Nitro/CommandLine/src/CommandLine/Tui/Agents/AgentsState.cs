using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tui.Agents;

/// <summary>
/// The loaded live participants, selected row, and focused pane.
/// </summary>
internal sealed class AgentsState(IAgentSessionRegistry sessionRegistry, IClaudeSessionActivityReader activityReader)
{
    /// <summary>
    /// Live participant rows in registry order by harness and session id, including
    /// unbound sessions. Ended or reaped sessions are absent after refresh.
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
    /// Reloads every live participant from the registry. The selected row stays selected by its
    /// <see cref="AgentSessionKey"/>; otherwise it is clamped to the new list's bounds.
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
    /// Attaches activity for an online Claude Code participant, or null activity
    /// for other participants.
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
