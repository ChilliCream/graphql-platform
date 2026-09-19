using ChilliCream.Nitro.CommandLine.Services.Memory;

namespace ChilliCream.Nitro.CommandLine.Tui.Memory;

/// <summary>
/// The selected collection, search text, loaded memory items, selection, and focus.
/// </summary>
internal sealed class MemoryState(MemoryDataLoader loader)
{
    /// <summary>
    /// The active collection, initially curated memories.
    /// </summary>
    public MemoryCollectionFilter Collection { get; private set; } = MemoryCollectionFilter.Curated;

    /// <summary>
    /// The search box text last applied to the loaded list, parsed by
    /// <see cref="MemoryQueryParser"/>.
    /// </summary>
    public string SearchText { get; private set; } = "";

    /// <summary>
    /// The curated memories currently loaded, populated when
    /// <see cref="Collection"/> is <see cref="MemoryCollectionFilter.Curated"/>.
    /// </summary>
    public IReadOnlyList<MemoryRecord> CuratedRecords { get; private set; } = [];

    /// <summary>
    /// The journal entries currently loaded, populated when
    /// <see cref="Collection"/> is <see cref="MemoryCollectionFilter.Journal"/>.
    /// </summary>
    public IReadOnlyList<MemoryJournalEntry> JournalEntries { get; private set; } = [];

    /// <summary>
    /// The index of the selected row within whichever list
    /// <see cref="Collection"/> currently shows.
    /// </summary>
    public int SelectedRow { get; set; }

    /// <summary>
    /// Which pane currently holds focus.
    /// </summary>
    public MemoryFocus Focus { get; set; } = MemoryFocus.List;

    /// <summary>
    /// The number of rows in whichever list <see cref="Collection"/>
    /// currently shows.
    /// </summary>
    public int ItemCount => Collection == MemoryCollectionFilter.Curated ? CuratedRecords.Count : JournalEntries.Count;

    /// <summary>
    /// A diagnostic message from the last <see cref="RefreshAsync"/> when the store
    /// rejected the read with an <see cref="ExitException"/>, or null otherwise.
    /// </summary>
    public string? LoadError { get; private set; }

    /// <summary>
    /// The curated memory at <see cref="SelectedRow"/>, or null when
    /// <see cref="Collection"/> is not <see cref="MemoryCollectionFilter.Curated"/>
    /// or the row is out of range.
    /// </summary>
    public MemoryRecord? SelectedCuratedRecord
        => Collection == MemoryCollectionFilter.Curated && SelectedRow >= 0 && SelectedRow < CuratedRecords.Count
            ? CuratedRecords[SelectedRow]
            : null;

    /// <summary>
    /// The journal entry at <see cref="SelectedRow"/>, or null when
    /// <see cref="Collection"/> is not <see cref="MemoryCollectionFilter.Journal"/>
    /// or the row is out of range.
    /// </summary>
    public MemoryJournalEntry? SelectedJournalEntry
        => Collection == MemoryCollectionFilter.Journal && SelectedRow >= 0 && SelectedRow < JournalEntries.Count
            ? JournalEntries[SelectedRow]
            : null;

    /// <summary>
    /// Reloads whichever list <see cref="Collection"/> currently shows for
    /// <see cref="SearchText"/>. The selected item
    /// stays selected when it is still present in the reloaded list;
    /// otherwise the selected row is clamped to the new list's bounds.
    /// </summary>
    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        var query = MemoryQueryParser.Parse(SearchText);
        var selectedId = Collection == MemoryCollectionFilter.Curated
            ? SelectedCuratedRecord?.Id
            : SelectedJournalEntry?.Id;

        try
        {
            if (Collection == MemoryCollectionFilter.Curated)
            {
                CuratedRecords = await loader.LoadCuratedAsync(query, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                JournalEntries = await loader.LoadJournalAsync(query, cancellationToken).ConfigureAwait(false);
            }

            LoadError = null;
        }
        catch (ExitException exception)
        {
            LoadError = exception.Message;
            ClearActiveCollection();
        }

        var preservedIndex = selectedId is null ? -1 : IndexOf(selectedId);
        SelectedRow = preservedIndex >= 0 ? preservedIndex : Math.Clamp(SelectedRow, 0, Math.Max(0, ItemCount - 1));
    }

    /// <summary>
    /// Toggles the collection when <paramref name="delta"/> is nonzero.
    /// Reloads the active collection even when the delta is zero.
    /// </summary>
    public async Task CycleCollectionAsync(int delta, CancellationToken cancellationToken)
    {
        if (delta != 0)
        {
            Collection = Collection == MemoryCollectionFilter.Curated
                ? MemoryCollectionFilter.Journal
                : MemoryCollectionFilter.Curated;
        }

        SelectedRow = 0;
        await RefreshAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Applies a new search box text and reloads.
    /// </summary>
    public async Task ApplySearchAsync(string text, CancellationToken cancellationToken)
    {
        SearchText = text;
        SelectedRow = 0;
        await RefreshAsync(cancellationToken).ConfigureAwait(false);
    }

    private void ClearActiveCollection()
    {
        if (Collection == MemoryCollectionFilter.Curated)
        {
            CuratedRecords = [];
        }
        else
        {
            JournalEntries = [];
        }
    }

    private int IndexOf(string id)
    {
        if (Collection == MemoryCollectionFilter.Curated)
        {
            for (var i = 0; i < CuratedRecords.Count; i++)
            {
                if (CuratedRecords[i].Id == id)
                {
                    return i;
                }
            }
        }
        else
        {
            for (var i = 0; i < JournalEntries.Count; i++)
            {
                if (JournalEntries[i].Id == id)
                {
                    return i;
                }
            }
        }

        return -1;
    }
}
