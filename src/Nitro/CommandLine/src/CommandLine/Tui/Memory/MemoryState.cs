using ChilliCream.Nitro.CommandLine.Services.Memory;

namespace ChilliCream.Nitro.CommandLine.Tui.Memory;

/// <summary>
/// The loaded curated-memory and journal rows, kind filter, search text, and selection for
/// the Memory tab.
/// </summary>
internal sealed class MemoryState(MemoryDataLoader loader)
{
    /// <summary>
    /// The active kind filter, initially every entry.
    /// </summary>
    public MemoryCollectionFilter Filter { get; private set; } = MemoryCollectionFilter.All;

    /// <summary>
    /// The search box text last applied to the loaded rows, parsed by
    /// <see cref="MemoryQueryParser"/>. Its <c>tag:</c> and <c>type:</c> terms narrow curated
    /// rows only; free text narrows both.
    /// </summary>
    public string SearchText { get; private set; } = string.Empty;

    /// <summary>
    /// Every row matching <see cref="Filter"/> and <see cref="SearchText"/>, curated and
    /// journal entries merged, sorted by <see cref="MemoryRow.Time"/> descending then id.
    /// </summary>
    public IReadOnlyList<MemoryRow> Rows { get; private set; } = [];

    /// <summary>
    /// The index of the selected row within <see cref="Rows"/>.
    /// </summary>
    public int SelectedRow { get; set; }

    /// <summary>
    /// A diagnostic message from the last <see cref="RefreshAsync"/> when the store rejected
    /// the read with an <see cref="ExitException"/>, or null otherwise.
    /// </summary>
    public string? LoadError { get; private set; }

    /// <summary>
    /// The row at <see cref="SelectedRow"/>, or null when there is no such row.
    /// </summary>
    public MemoryRow? SelectedItem
        => SelectedRow >= 0 && SelectedRow < Rows.Count ? Rows[SelectedRow] : null;

    /// <summary>
    /// Reloads every row matching <see cref="Filter"/> and <see cref="SearchText"/>. The
    /// selected row stays selected by id when it is still present; otherwise the selection is
    /// clamped to the new list's bounds.
    /// </summary>
    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        var selectedId = SelectedItem?.Id;

        try
        {
            Rows = await LoadRowsAsync(cancellationToken).ConfigureAwait(false);
            LoadError = null;
        }
        catch (ExitException exception)
        {
            LoadError = exception.Message;
            Rows = [];
        }

        var preservedIndex = selectedId is null ? -1 : IndexOf(selectedId);
        SelectedRow = preservedIndex >= 0 ? preservedIndex : Math.Clamp(SelectedRow, 0, Math.Max(0, Rows.Count - 1));
    }

    /// <summary>
    /// Cycles <see cref="Filter"/> through All, Curated, and Journal when <paramref name="delta"/>
    /// is nonzero, reloads, and selects the first row.
    /// </summary>
    public async Task CycleFilterAsync(int delta, CancellationToken cancellationToken)
    {
        if (delta != 0)
        {
            var values = Enum.GetValues<MemoryCollectionFilter>();
            var currentIndex = Array.IndexOf(values, Filter);
            var nextIndex = ((currentIndex + delta) % values.Length + values.Length) % values.Length;
            Filter = values[nextIndex];
        }

        SelectedRow = 0;
        await RefreshAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Applies a new search box text and reloads.
    /// </summary>
    public async Task ApplySearchAsync(string text, CancellationToken cancellationToken)
    {
        SearchText = text.Trim();
        SelectedRow = 0;
        await RefreshAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<MemoryRow>> LoadRowsAsync(CancellationToken cancellationToken)
    {
        var query = MemoryQueryParser.Parse(SearchText);
        var rows = new List<MemoryRow>();

        if (Filter != MemoryCollectionFilter.Journal)
        {
            var curated = await loader.LoadCuratedAsync(query, cancellationToken).ConfigureAwait(false);
            rows.AddRange(curated.Select(ToRow));
        }

        if (Filter != MemoryCollectionFilter.Curated)
        {
            var journal = await loader.LoadJournalAsync(query, cancellationToken).ConfigureAwait(false);
            rows.AddRange(journal.Select(ToRow));
        }

        return rows.OrderByDescending(r => r.Time).ThenBy(r => r.Id, StringComparer.Ordinal).ToList();
    }

    private static MemoryRow ToRow(MemoryRecord record) =>
        new(MemoryCollectionFilter.Curated, record.Id, record.Type, record.Tags, record.Body, record.UpdatedAt);

    private static MemoryRow ToRow(MemoryJournalEntry entry) =>
        new(MemoryCollectionFilter.Journal, entry.Id, Type: null, Tags: [], entry.Body, entry.CreatedAt);

    private int IndexOf(string id)
    {
        for (var i = 0; i < Rows.Count; i++)
        {
            if (Rows[i].Id == id)
            {
                return i;
            }
        }

        return -1;
    }
}
