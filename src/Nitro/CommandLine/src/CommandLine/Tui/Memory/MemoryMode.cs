using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Tui.Editing;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Shell;
using ChilliCream.Nitro.CommandLine.Tui.Widgets;
using ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;
using Spectre.Console.Rendering;
using ConfirmDialog = ChilliCream.Nitro.CommandLine.Tui.Editing.ConfirmDialog;
using CursorDirection = ChilliCream.Nitro.CommandLine.Tui.Input.CursorDirection;

namespace ChilliCream.Nitro.CommandLine.Tui.Memory;

/// <summary>
/// The memory board <see cref="ITuiMode"/>: a list pane of curated memories
/// or journal entries next to a detail pane for the selected item, modeled
/// on the mail board's list/detail split (see <see cref="MemoryFocus"/> and
/// <c>MailMode</c>/<c>MailFocus</c>). f cycles between the curated and
/// journal collections, s cycles the scope filter, and / opens the search
/// box; every list read goes through <see cref="MemoryDataLoader"/>, which
/// wraps the same <see cref="IMemoryStore"/> reads the CLI's <c>recent</c>
/// and <c>search</c> commands use, so no second query path exists. Beyond
/// browsing, the tab supports exactly two writes: p promotes the selected
/// journal entry and d forgets (hard-deletes) the selected curated memory,
/// both going through the same store members the CLI's own commands call;
/// there is no inline editing. This mode owns its own modal overlays (the
/// search box, the promote form, the forget confirmation, and their shared
/// discard confirmation) rather than routing through <see cref="TuiShell"/>'s
/// task-specific overlay fields.
/// </summary>
internal sealed class MemoryMode : ITuiMode, IRawKeyCapturingMode
{
    private const int PanelChromeWidth = 4;
    private const int PanelChromeHeight = 2;
    private const int MaxIndicatorSettlePasses = 3;
    private const int ListWidthNumerator = 2;
    private const int ListWidthDenominator = 5;

    private readonly IMemoryStore _store;
    private readonly MemoryState _state;
    private readonly MemoryDetailView _detailView = new();
    private readonly TimeProvider _timeProvider;
    private readonly Viewport _listViewport = new(0, 0);

    private MemorySearchForm? _searchForm;
    private MemoryPromoteForm? _promoteForm;
    private ConfirmDialog? _forgetDialog;
    private MemoryRecord? _forgetTarget;
    private ConfirmDialog? _discardDialog;
    private bool _pendingRefresh;

    public MemoryMode(IMemoryStore store, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(store);

        _store = store;
        _state = new MemoryState(new MemoryDataLoader(store));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// The board's current live state: loaded items, collection, scope,
    /// selection, and focus.
    /// </summary>
    public MemoryState State => _state;

    /// <inheritdoc />
    public bool IsInputCapturing
        => _discardDialog is not null
        || _forgetDialog is not null
        || _promoteForm is not null
        || _searchForm is not null;

    /// <inheritdoc />
    public IReadOnlyList<KeyHint> CapturingHints
        => _discardDialog is not null ? ConfirmDialog.Hints
        : _forgetDialog is not null ? ConfirmDialog.Hints
        : _promoteForm is not null ? MemoryPromoteForm.Hints
        : _searchForm is not null ? MemorySearchForm.Hints
        : [];

    /// <inheritdoc />
    public KeyMap? KeyMap => null;

    /// <inheritdoc />
    /// <remarks>
    /// Defers the blocking store read: it only marks a refresh pending,
    /// performed lazily on the first <see cref="Render"/> or
    /// <see cref="Handle"/> call. Memory's tab title is static, so nothing
    /// in the tab strip needs the data eagerly at tab-construction time.
    /// </remarks>
    public void OnEnter() => _pendingRefresh = true;

    /// <inheritdoc />
    public void OnResize(int width, int height)
    {
        // Render(width, height) recomputes the layout and every pane's
        // viewport window from its parameters on every frame, so there is
        // no per-resize state to update ahead of time.
    }

    /// <inheritdoc />
    public IReadOnlyList<TuiMessage> Handle(TuiMessage message)
    {
        EnsureLoaded();

        return message switch
        {
            TuiMessage.MoveCursor(CursorDirection.Up) => MoveOrScroll(-1),
            TuiMessage.MoveCursor(CursorDirection.Down) => MoveOrScroll(1),
            TuiMessage.MoveCursor(CursorDirection.Left) => TogglePane(),
            TuiMessage.MoveCursor(CursorDirection.Right) => TogglePane(),
            TuiMessage.MoveToEdge(var edge) => MoveOrScrollToEdge(edge),
            TuiMessage.OpenSelected => FocusDetail(),
            TuiMessage.RefreshRequested => Refresh(),
            TuiMessage.CycleView(var delta) => CycleCollection(delta),
            TuiMessage.CycleScopeRequested => CycleScope(),
            TuiMessage.SearchRequested => OpenSearchForm(),
            TuiMessage.PromoteRequested => OpenPromoteForm(),
            TuiMessage.ForgetRequested => OpenForgetDialog(),
            TuiMessage.CopySelectedId => CopySelectedId(),
            _ => []
        };
    }

    /// <summary>
    /// Handles one raw key while <see cref="IsInputCapturing"/> is true:
    /// routed here by the host instead of through the semantic
    /// <see cref="TuiMessage"/> dispatch, since the active overlay's text
    /// fields need raw characters, not key-bound intents.
    /// </summary>
    public IReadOnlyList<TuiMessage> HandleRawKey(ConsoleKeyInfo info)
    {
        if (_discardDialog is not null)
        {
            return HandleDiscardDialogKey(info);
        }

        if (_forgetDialog is not null)
        {
            return HandleForgetDialogKey(info);
        }

        if (_promoteForm is not null)
        {
            return HandlePromoteFormKey(info);
        }

        if (_searchForm is not null)
        {
            return HandleSearchFormKey(info);
        }

        return [];
    }

    /// <inheritdoc />
    public IRenderable Render(int width, int height)
    {
        EnsureLoaded();

        if (width <= 0 || height <= 0)
        {
            return new Markup(string.Empty);
        }

        if (_discardDialog is { } discardDialog)
        {
            return discardDialog.Render(width, height);
        }

        if (_forgetDialog is { } forgetDialog)
        {
            return forgetDialog.Render(width, height);
        }

        if (_promoteForm is { } promoteForm)
        {
            return promoteForm.Render(width, height);
        }

        if (_searchForm is { } searchForm)
        {
            return searchForm.Render(width, height);
        }

        var listWidth = Math.Max(1, width * ListWidthNumerator / ListWidthDenominator);
        var detailWidth = Math.Max(1, width - listWidth);

        return new Layout("memory").SplitColumns(
            new Layout("list", RenderListPane(listWidth, height)).Size(listWidth),
            new Layout("detail", RenderDetailPane(detailWidth, height)));
    }

    private IReadOnlyList<TuiMessage> MoveOrScroll(int delta)
    {
        if (_state.Focus == MemoryFocus.List)
        {
            if (_state.ItemCount > 0)
            {
                _state.SelectedRow = Math.Clamp(_state.SelectedRow + delta, 0, _state.ItemCount - 1);
            }
        }
        else if (delta > 0)
        {
            _detailView.ScrollDown();
        }
        else
        {
            _detailView.ScrollUp();
        }

        return [];
    }

    private IReadOnlyList<TuiMessage> MoveOrScrollToEdge(EdgeTarget edge)
    {
        if (_state.Focus == MemoryFocus.List)
        {
            if (_state.ItemCount > 0)
            {
                _state.SelectedRow = edge == EdgeTarget.Top ? 0 : _state.ItemCount - 1;
            }
        }
        else if (edge == EdgeTarget.Top)
        {
            _detailView.ScrollToTop();
        }
        else
        {
            _detailView.ScrollToBottom();
        }

        return [];
    }

    private IReadOnlyList<TuiMessage> TogglePane()
    {
        _state.Focus = _state.Focus == MemoryFocus.List ? MemoryFocus.Detail : MemoryFocus.List;
        return [];
    }

    private IReadOnlyList<TuiMessage> FocusDetail()
    {
        _state.Focus = MemoryFocus.Detail;
        return [];
    }

    private IReadOnlyList<TuiMessage> Refresh()
    {
        RefreshBlocking();
        return [];
    }

    private IReadOnlyList<TuiMessage> CycleCollection(int delta)
    {
        _state.CycleCollectionAsync(delta, CancellationToken.None).GetAwaiter().GetResult();
        _detailView.ResetScroll();
        return [];
    }

    private IReadOnlyList<TuiMessage> CycleScope()
    {
        _state.CycleScopeAsync(CancellationToken.None).GetAwaiter().GetResult();
        _detailView.ResetScroll();
        return [];
    }

    private IReadOnlyList<TuiMessage> OpenSearchForm()
    {
        _searchForm = new MemorySearchForm(_state.SearchText);
        return [];
    }

    private IReadOnlyList<TuiMessage> HandleSearchFormKey(ConsoleKeyInfo info)
    {
        var result = _searchForm!.HandleKey(info);

        return result switch
        {
            null => [],
            FormResult.Cancelled => CloseSearchForm(),
            FormResult.ButtonActivated { ButtonId: MemorySearchForm.CancelButtonId } => CloseSearchForm(),
            FormResult.Submitted => ApplySearch(),
            _ => []
        };
    }

    private IReadOnlyList<TuiMessage> CloseSearchForm()
    {
        _searchForm = null;
        return [];
    }

    private IReadOnlyList<TuiMessage> ApplySearch()
    {
        var text = _searchForm!.Text;
        _searchForm = null;

        _state.ApplySearchAsync(text, CancellationToken.None).GetAwaiter().GetResult();
        _detailView.ResetScroll();

        return [];
    }

    private IReadOnlyList<TuiMessage> OpenPromoteForm()
    {
        if (_state.SelectedJournalEntry is not { } entry)
        {
            return [new TuiMessage.ShowToast("No journal entry selected.", ToastStyle.Warn)];
        }

        _promoteForm = new MemoryPromoteForm(entry);
        return [];
    }

    private IReadOnlyList<TuiMessage> HandlePromoteFormKey(ConsoleKeyInfo info)
    {
        var result = _promoteForm!.HandleKey(info);

        return result switch
        {
            null => [],
            FormResult.Cancelled => TryDiscardPromote(),
            FormResult.ButtonActivated { ButtonId: MemoryPromoteForm.CancelButtonId } => TryDiscardPromote(),
            FormResult.Submitted submitted => SubmitPromote(submitted),
            _ => []
        };
    }

    private IReadOnlyList<TuiMessage> TryDiscardPromote()
    {
        if (_promoteForm!.IsDirty)
        {
            _discardDialog = CreateDiscardDialog();
            return [];
        }

        _promoteForm = null;
        return [];
    }

    private IReadOnlyList<TuiMessage> SubmitPromote(FormResult.Submitted submitted)
    {
        var outcome = _promoteForm!.SubmitAsync(_store, submitted.Values, CancellationToken.None)
            .GetAwaiter().GetResult();

        if (outcome is not MemoryPromoteOutcome.Succeeded)
        {
            return [outcome.ToShowToast()];
        }

        _promoteForm = null;
        RefreshBlocking();

        return [outcome.ToShowToast()];
    }

    private IReadOnlyList<TuiMessage> OpenForgetDialog()
    {
        if (_state.SelectedCuratedRecord is not { } record)
        {
            return [new TuiMessage.ShowToast("No curated memory selected.", ToastStyle.Warn)];
        }

        _forgetTarget = record;
        _forgetDialog = MemoryLifecycleActions.CreateForgetDialog(record);
        return [];
    }

    private IReadOnlyList<TuiMessage> HandleForgetDialogKey(ConsoleKeyInfo info)
    {
        var result = _forgetDialog!.HandleKey(info);

        return result switch
        {
            null => [],
            ConfirmDialogResult.Cancelled => CancelForgetDialog(),
            ConfirmDialogResult.Confirmed => SubmitForget(),
            _ => []
        };
    }

    private IReadOnlyList<TuiMessage> CancelForgetDialog()
    {
        _forgetDialog = null;
        _forgetTarget = null;
        return [];
    }

    private IReadOnlyList<TuiMessage> SubmitForget()
    {
        var target = _forgetTarget!;
        _forgetDialog = null;
        _forgetTarget = null;

        var outcome = MemoryLifecycleActions.ForgetAsync(_store, target, CancellationToken.None)
            .GetAwaiter().GetResult();

        RefreshBlocking();

        return [outcome.ToShowToast()];
    }

    private IReadOnlyList<TuiMessage> HandleDiscardDialogKey(ConsoleKeyInfo info)
    {
        var result = _discardDialog!.HandleKey(info);

        return result switch
        {
            null => [],
            ConfirmDialogResult.Confirmed => ConfirmDiscard(),
            ConfirmDialogResult.Cancelled => CancelDiscard(),
            _ => []
        };
    }

    private IReadOnlyList<TuiMessage> ConfirmDiscard()
    {
        _discardDialog = null;
        _promoteForm = null;
        return [];
    }

    private IReadOnlyList<TuiMessage> CancelDiscard()
    {
        _discardDialog = null;
        return [];
    }

    private static ConfirmDialog CreateDiscardDialog()
        => new("Discard unsaved changes?", "Discard", ButtonKind.Danger);

    private IReadOnlyList<TuiMessage> CopySelectedId()
    {
        var id = _state.Collection == MemoryCollectionFilter.Curated
            ? _state.SelectedCuratedRecord?.Id
            : _state.SelectedJournalEntry?.Id;

        return id is null
            ? [new TuiMessage.ShowToast("No item selected.", ToastStyle.Warn)]
            : [new TuiMessage.ShowToast(id, ToastStyle.Info)];
    }

    private IRenderable RenderListPane(int width, int height)
    {
        var focused = _state.Focus == MemoryFocus.List;
        var safeWidth = Math.Max(1, width);
        var contentWidth = Math.Max(0, safeWidth - PanelChromeWidth);
        var interiorHeight = Math.Max(0, height - PanelChromeHeight);

        var lines = RenderListLines(contentWidth, interiorHeight, focused);
        var panel = ColumnPane.Render(ListTitle(), _state.ItemCount, lines, focused);
        panel.Width = safeWidth;
        panel.Height = Math.Max(1, height);

        return panel;
    }

    private string ListTitle()
    {
        var collectionName = _state.Collection == MemoryCollectionFilter.Curated ? "Curated" : "Journal";

        if (_state.Collection == MemoryCollectionFilter.Curated)
        {
            var suffix = _state.SearchText.Length > 0 ? " (filtered)" : "";
            return $"{collectionName} · {_state.Scope}{suffix}";
        }

        var parsed = MemoryQueryParser.Parse(_state.SearchText);
        var filteredSuffix = parsed.Text.Length > 0 ? " (filtered)" : "";
        var ignoredSuffix = parsed.Type is not null || parsed.Tags.Count > 0 ? " (type/tag ignored)" : "";
        return $"{collectionName} · {_state.Scope}{filteredSuffix}{ignoredSuffix}";
    }

    private IRenderable RenderDetailPane(int width, int height)
        => _detailView.Render(_state, width, height, _state.Focus == MemoryFocus.Detail);

    private IReadOnlyList<string> RenderListLines(int contentWidth, int interiorHeight, bool focused)
        => _state.Collection == MemoryCollectionFilter.Curated
            ? RenderCuratedListLines(contentWidth, interiorHeight, focused)
            : RenderJournalListLines(contentWidth, interiorHeight, focused);

    private IReadOnlyList<string> RenderCuratedListLines(int contentWidth, int interiorHeight, bool focused)
    {
        if (interiorHeight <= 0)
        {
            return [];
        }

        var records = _state.CuratedRecords;
        var (start, visibleCount) = SliceViewport(records.Count, interiorHeight);
        var now = _timeProvider.GetUtcNow();

        var visibleRecords = new List<MemoryRecord>(visibleCount);

        for (var i = 0; i < visibleCount; i++)
        {
            visibleRecords.Add(records[start + i]);
        }

        var widths = MemoryRowBadge.ComputeWidths(visibleRecords, now);
        var lines = new List<string>(interiorHeight);

        if (_listViewport.HiddenAbove > 0)
        {
            lines.Add(FormatIndicator(_listViewport.HiddenAbove, "above"));
        }

        for (var i = 0; i < visibleCount; i++)
        {
            var selected = focused && start + i == _state.SelectedRow;
            lines.Add(MemoryRowBadge.Render(visibleRecords[i], now, selected, contentWidth, widths));
        }

        if (_listViewport.HiddenBelow > 0)
        {
            lines.Add(FormatIndicator(_listViewport.HiddenBelow, "below"));
        }

        return PadLines(lines, interiorHeight);
    }

    private IReadOnlyList<string> RenderJournalListLines(int contentWidth, int interiorHeight, bool focused)
    {
        if (interiorHeight <= 0)
        {
            return [];
        }

        var entries = _state.JournalEntries;
        var (start, visibleCount) = SliceViewport(entries.Count, interiorHeight);

        var visibleEntries = new List<MemoryJournalEntry>(visibleCount);

        for (var i = 0; i < visibleCount; i++)
        {
            visibleEntries.Add(entries[start + i]);
        }

        var widths = MemoryJournalRowBadge.ComputeWidths(visibleEntries);
        var lines = new List<string>(interiorHeight);

        if (_listViewport.HiddenAbove > 0)
        {
            lines.Add(FormatIndicator(_listViewport.HiddenAbove, "above"));
        }

        for (var i = 0; i < visibleCount; i++)
        {
            var selected = focused && start + i == _state.SelectedRow;
            lines.Add(MemoryJournalRowBadge.Render(visibleEntries[i], selected, contentWidth, widths));
        }

        if (_listViewport.HiddenBelow > 0)
        {
            lines.Add(FormatIndicator(_listViewport.HiddenBelow, "below"));
        }

        return PadLines(lines, interiorHeight);
    }

    /// <summary>
    /// Resolves the visible window into a list of <paramref name="itemCount"/>
    /// items for <see cref="_listViewport"/>, reserving rows for "N more
    /// above/below" indicators once the items no longer fit
    /// <paramref name="interiorHeight"/>.
    /// </summary>
    private (int Start, int Count) SliceViewport(int itemCount, int interiorHeight)
    {
        var reservedRows = 0;

        for (var pass = 0; pass < MaxIndicatorSettlePasses; pass++)
        {
            var windowHeight = Math.Max(0, interiorHeight - reservedRows);
            _listViewport.Update(itemCount, windowHeight);
            _listViewport.EnsureVisible(_state.SelectedRow);

            var needed = (_listViewport.HiddenAbove > 0 ? 1 : 0) + (_listViewport.HiddenBelow > 0 ? 1 : 0);

            if (needed == reservedRows)
            {
                break;
            }

            reservedRows = needed;
        }

        return _listViewport.Slice();
    }

    private static IReadOnlyList<string> PadLines(List<string> lines, int interiorHeight)
    {
        while (lines.Count < interiorHeight)
        {
            lines.Add(string.Empty);
        }

        return lines;
    }

    private static string FormatIndicator(int hiddenCount, string direction) => $"  {hiddenCount} more {direction}";

    private void RefreshBlocking() => _state.RefreshAsync(CancellationToken.None).GetAwaiter().GetResult();

    /// <summary>
    /// Performs the refresh <see cref="OnEnter"/> deferred, exactly once,
    /// the first time <see cref="Render"/> or <see cref="Handle"/> runs
    /// after entering the tab.
    /// </summary>
    private void EnsureLoaded()
    {
        if (!_pendingRefresh)
        {
            return;
        }

        _pendingRefresh = false;
        RefreshBlocking();
    }
}
