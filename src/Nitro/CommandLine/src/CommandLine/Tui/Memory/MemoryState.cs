using ChilliCream.Nitro.CommandLine.Services.Memory;

namespace ChilliCream.Nitro.CommandLine.Tui.Memory;

/// <summary>
/// The memory tab's live state: which collection and scope are shown, the
/// current search box text, the loaded curated or journal entries for them,
/// and which row and pane are selected and focused.
/// </summary>
internal sealed class MemoryState(MemoryDataLoader loader)
{
    private static readonly List<string> ScopeCycle = [MemoryScopes.All, MemoryScopes.Project, MemoryScopes.Global];

    /// <summary>
    /// Which collection <see cref="CuratedRecords"/>/<see cref="JournalEntries"/>
    /// is populated for.
    /// </summary>
    public MemoryCollectionFilter Collection { get; private set; } = MemoryCollectionFilter.Curated;

    /// <summary>
    /// The scope reads are narrowed to: one of <see cref="MemoryScopes.All"/>,
    /// <see cref="MemoryScopes.Project"/>, or <see cref="MemoryScopes.Global"/>.
    /// </summary>
    public string Scope { get; private set; } = MemoryScopes.All;

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
    /// A diagnostic message from the last <see cref="RefreshAsync"/>, set
    /// when the read hit invalid data: either <see cref="Scope"/> is
    /// <see cref="MemoryScopes.All"/> and a cross-scope duplicate id makes
    /// the merged read invalid data (per the store's own contract, no
    /// partial result is served), or the store rejected a file it read with
    /// an <see cref="ExitException"/> such as malformed frontmatter; null
    /// otherwise. Mirrors how <c>MailUnavailableMode</c> keeps a hard read
    /// failure from crashing the tab, shown inline in the list pane instead
    /// of replacing the whole mode, since unlike a failed mail actor
    /// resolution this can recur on every refresh rather than only at tab
    /// construction.
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
    /// <see cref="Scope"/> and <see cref="SearchText"/>. The selected item
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
                CuratedRecords = await loader.LoadCuratedAsync(Scope, query, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                JournalEntries = await loader.LoadJournalAsync(Scope, query, cancellationToken).ConfigureAwait(false);
            }

            LoadError = null;
        }
        catch (MemoryScopeConflictException exception)
        {
            LoadError = FormatConflictMessage(exception);
            ClearActiveCollection();
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
    /// Switches <see cref="Collection"/> to the other value (the only two
    /// values, so any nonzero delta flips it) and reloads.
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
    /// Advances <see cref="Scope"/> to the next value in the all, project,
    /// global cycle and reloads.
    /// </summary>
    public async Task CycleScopeAsync(CancellationToken cancellationToken)
    {
        var index = ScopeCycle.IndexOf(Scope);
        Scope = ScopeCycle[(index + 1) % ScopeCycle.Count];
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

    private static string FormatConflictMessage(MemoryScopeConflictException exception)
    {
        var ids = string.Join(", ", exception.Conflicts.Select(c => c.Id));
        return $"Cross-scope duplicate id(s), narrow --scope or run memory doctor: {ids}";
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
