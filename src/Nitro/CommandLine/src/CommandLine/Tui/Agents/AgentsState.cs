using System.Text;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tui.Mail;

namespace ChilliCream.Nitro.CommandLine.Tui.Agents;

/// <summary>
/// The loaded agent rows, name search filter, and selection for the Agents tab.
/// </summary>
internal sealed class AgentsState(IAgentStore store, TimeProvider timeProvider)
{
    private IReadOnlyList<AgentRow> _allRows = [];
    private string _lastSignature = string.Empty;

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
        ApplyFilterAndSort(timeProvider.GetUtcNow());

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
        ApplyFilterAndSort(timeProvider.GetUtcNow());
        SelectedRow = 0;
    }

    /// <summary>
    /// Recomputes every loaded row's resolved state and formatted started/last-seen ages as
    /// of <paramref name="now"/>. When any changed since the last call, re-applies the filter
    /// and sort and preserves the selected agent by name, returning true; returns false
    /// without touching <see cref="Rows"/> or <see cref="SelectedRow"/> when nothing changed.
    /// </summary>
    public bool Resettle(DateTimeOffset now)
    {
        var signature = ComputeSignature(now);

        if (signature == _lastSignature)
        {
            return false;
        }

        var selectedName = SelectedAgent?.Name;

        ApplyFilterAndSort(now);

        var preservedIndex = selectedName is null ? -1 : IndexOf(Rows, selectedName);

        SelectedRow = preservedIndex >= 0
            ? preservedIndex
            : Math.Clamp(SelectedRow, 0, Math.Max(0, Rows.Count - 1));

        return true;
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

    private void ApplyFilterAndSort(DateTimeOffset now)
    {
        _lastSignature = ComputeSignature(now);

        var filtered = SearchText.Length == 0
            ? _allRows
            : _allRows.Where(row => row.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        Rows = filtered
            .OrderBy(row => StateRank(AgentStateResolver.Resolve(row, now)))
            .ThenByDescending(row => row.LastSeenAt)
            .ThenBy(row => row.Name, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Builds a cheap signature of every loaded row's resolved state and formatted
    /// started/last-seen ages as of <paramref name="now"/>, used to detect whether a tick
    /// changed anything worth re-sorting and re-rendering.
    /// </summary>
    private string ComputeSignature(DateTimeOffset now)
    {
        var signature = new StringBuilder();

        foreach (var row in _allRows)
        {
            signature
                .Append(row.Name).Append('\u0001')
                .Append(AgentStateResolver.Resolve(row, now)).Append('\u0001')
                .Append(MailAges.Format(row.StartedAt, now)).Append('\u0001')
                .Append(MailAges.Format(row.LastSeenAt, now)).Append('\u0002');
        }

        return signature.ToString();
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
