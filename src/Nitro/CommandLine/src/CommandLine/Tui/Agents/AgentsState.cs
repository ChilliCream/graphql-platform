using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tui.Agents;

/// <summary>
/// The loaded agent rows, name search filter, and selection for the Agents tab.
/// </summary>
internal sealed class AgentsState(IAgentStore store, TimeProvider timeProvider)
{
    private IReadOnlyList<AgentRow> _allRows = [];

    /// <summary>
    /// Every non-deleted agent matching <see cref="SearchText"/>, sorted by state group
    /// (Online, Unreachable, Offline) as of the last <see cref="RefreshAsync"/> or
    /// <see cref="ApplySearch"/> call, then by last seen descending, then by name.
    /// </summary>
    public IReadOnlyList<AgentRow> Rows { get; private set; } = [];

    /// <summary>
    /// The total non-deleted agent count, independent of <see cref="SearchText"/>.
    /// </summary>
    public int TotalCount => _allRows.Count;

    /// <summary>
    /// The index of the selected row within <see cref="Rows"/>.
    /// </summary>
    public int SelectedRow { get; set; }

    /// <summary>
    /// The current name search filter; empty shows every agent.
    /// </summary>
    public string SearchText { get; private set; } = string.Empty;

    /// <summary>
    /// The row at <see cref="SelectedRow"/>, or null when there is no such row.
    /// </summary>
    public AgentRow? SelectedAgent
        => SelectedRow >= 0 && SelectedRow < Rows.Count ? Rows[SelectedRow] : null;

    /// <summary>
    /// Reloads every non-deleted agent from the store, re-applies the search filter and
    /// sort, and keeps the previously selected agent selected by name when it is still
    /// present; otherwise clamps the selection to the new list's bounds.
    /// </summary>
    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        var selectedName = SelectedAgent?.Name;

        _allRows = await store.ListAsync(cancellationToken);
        ApplyFilterAndSort();

        var preservedIndex = selectedName is null ? -1 : IndexOf(Rows, selectedName);

        SelectedRow = preservedIndex >= 0
            ? preservedIndex
            : Math.Clamp(SelectedRow, 0, Math.Max(0, Rows.Count - 1));
    }

    /// <summary>
    /// Applies a new name search filter, re-sorts, and selects the first matching row.
    /// </summary>
    public void ApplySearch(string text)
    {
        SearchText = text.Trim();
        ApplyFilterAndSort();
        SelectedRow = 0;
    }

    /// <summary>
    /// Counts the agents currently resolving to <see cref="AgentState.Offline"/> as of
    /// <paramref name="now"/>, among every non-deleted agent, ignoring the search filter.
    /// </summary>
    public int CountOffline(DateTimeOffset now)
        => _allRows.Count(row => AgentStateResolver.Resolve(row, now) == AgentState.Offline);

    /// <summary>
    /// Counts the agents currently resolving to <see cref="AgentState.Online"/> as of
    /// <paramref name="now"/>, among every non-deleted agent, ignoring the search filter.
    /// </summary>
    public int CountOnline(DateTimeOffset now)
        => _allRows.Count(row => AgentStateResolver.Resolve(row, now) == AgentState.Online);

    private void ApplyFilterAndSort()
    {
        var now = timeProvider.GetUtcNow();

        var filtered = SearchText.Length == 0
            ? _allRows
            : _allRows.Where(row => row.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        Rows = filtered
            .OrderBy(row => StateRank(AgentStateResolver.Resolve(row, now)))
            .ThenByDescending(row => row.LastSeenAt)
            .ThenBy(row => row.Name, StringComparer.Ordinal)
            .ToList();
    }

    private static int StateRank(AgentState state) => state switch
    {
        AgentState.Online => 0,
        AgentState.Unreachable => 1,
        _ => 2
    };

    private static int IndexOf(IReadOnlyList<AgentRow> rows, string name)
    {
        for (var i = 0; i < rows.Count; i++)
        {
            if (rows[i].Name == name)
            {
                return i;
            }
        }

        return -1;
    }
}
