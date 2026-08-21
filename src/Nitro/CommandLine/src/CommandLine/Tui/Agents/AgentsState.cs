using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tui.Agents;

/// <summary>
/// The live state of the agents list: every registered agent, in the order
/// the registry returns them, plus which row is selected.
/// </summary>
internal sealed class AgentsState(IAgentRegistry registry)
{
    /// <summary>
    /// The agents currently loaded, ordered by name (the registry's own
    /// order).
    /// </summary>
    public IReadOnlyList<AgentRecord> Agents { get; private set; } = [];

    /// <summary>
    /// The index of the selected row within <see cref="Agents"/>.
    /// </summary>
    public int SelectedRow { get; set; }

    /// <summary>
    /// The agent at <see cref="SelectedRow"/>, or null when the list is
    /// empty or the row is out of range.
    /// </summary>
    public AgentRecord? SelectedAgent
        => SelectedRow >= 0 && SelectedRow < Agents.Count ? Agents[SelectedRow] : null;

    /// <summary>
    /// Reloads every agent from the registry. The selected agent stays
    /// selected when it is still present in the reloaded list; otherwise the
    /// selected row is clamped to the new list's bounds.
    /// </summary>
    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        var selectedName = SelectedAgent?.Name;

        var agents = await registry.ListAsync(role: null, staleBefore: null, cancellationToken);
        Agents = agents;

        var preservedIndex = selectedName is null ? -1 : IndexOf(agents, selectedName);

        SelectedRow = preservedIndex >= 0
            ? preservedIndex
            : Math.Clamp(SelectedRow, 0, Math.Max(0, agents.Count - 1));
    }

    private static int IndexOf(IReadOnlyList<AgentRecord> agents, string name)
    {
        for (var i = 0; i < agents.Count; i++)
        {
            if (agents[i].Name == name)
            {
                return i;
            }
        }

        return -1;
    }
}
