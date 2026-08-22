using ChilliCream.Nitro.CommandLine.Services.Mail;

namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// The mail board's live state: the acting agent's messages and thread
/// rollups for the current mailbox and filter, the selected row, which pane
/// has focus, and the detail pane's view mode.
/// </summary>
/// <remarks>
/// <see cref="Rows"/> is the single navigation surface <see cref="SelectedRow"/>
/// indexes, regardless of <see cref="ListMode"/>: in <see cref="MailListMode.Flat"/>
/// it is one <see cref="MailListRow.MessageRow"/> per <see cref="Messages"/>
/// entry; in <see cref="MailListMode.Threads"/> it is one
/// <see cref="MailListRow.Thread"/> per <see cref="Threads"/> entry, followed
/// by that thread's messages as indented <see cref="MailListRow.MessageRow"/>
/// rows when the thread id is in the fold set. Assigning <see cref="SelectedRow"/>
/// synchronously resolves the detail pane's <see cref="ViewMode"/>,
/// <see cref="ThreadMessages"/>, and <see cref="SelectedMessage"/> for the
/// newly-selected row (a thread row defaults the detail pane to
/// <see cref="MailViewMode.Thread"/>, per the epic's layout ruling, and
/// <see cref="SelectedMessage"/> resolves to that thread's most recent
/// message so the u/a/r gestures still have a concrete message to act on);
/// <see cref="ShowThreadAsync"/> and <see cref="ShowMessage"/> remain a
/// manual, per-selection override of that default, exactly as before this
/// epic, since they only ever touch <see cref="ViewMode"/> and
/// <see cref="ThreadMessages"/>, which this class still owns.
/// </remarks>
internal sealed class MailState(string actor, MailDataLoader loader)
{
    /// <summary>
    /// The acting agent whose mail this state loads.
    /// </summary>
    public string Actor { get; } = actor;

    /// <summary>
    /// The mailbox currently selected. Changed only by
    /// <see cref="SelectMailboxAsync"/>, a direct jump independent of
    /// <see cref="Filter"/>. Defaults to <see cref="MailMailbox.Workspace"/>
    /// per the epic's user ruling.
    /// </summary>
    public MailMailbox Mailbox { get; private set; } = MailMailbox.Workspace;

    /// <summary>
    /// The read-state filter applied to <see cref="Messages"/> within
    /// <see cref="MailMailbox.Inbox"/>. Carried but not applied to any other
    /// <see cref="Mailbox"/>, and not applied to <see cref="Threads"/> in any
    /// mailbox: the store exposes no filtered thread query, so
    /// <see cref="MailListMode.Threads"/> within Inbox always shows the full
    /// inbox thread set regardless of <see cref="Filter"/>. The f/F gesture
    /// still cycles this value (the epic's non-goal keeps the axis), it just
    /// has no visible effect until <see cref="ListMode"/> is
    /// <see cref="MailListMode.Flat"/> and <see cref="Mailbox"/> is Inbox.
    /// </summary>
    public MailListFilter Filter { get; private set; } = MailListFilter.Inbox;

    /// <summary>
    /// The agent <see cref="Messages"/> and <see cref="Threads"/> are
    /// narrowed to (sent or received) within <see cref="MailMailbox.Workspace"/>,
    /// or null for every agent. Set by <see cref="SelectAgentFilterAsync"/>,
    /// and cleared whenever <see cref="SelectMailboxAsync"/> leaves
    /// <see cref="MailMailbox.Workspace"/> for another mailbox; belongs to
    /// Workspace only, since no other mailbox is already scoped to every
    /// agent.
    /// </summary>
    public string? AgentFilter { get; private set; }

    /// <summary>
    /// Which shape <see cref="Rows"/> renders in. Defaults to
    /// <see cref="MailListMode.Threads"/> per the epic's user ruling;
    /// <see cref="ToggleListMode"/> (Shift+V) switches back and forth.
    /// </summary>
    public MailListMode ListMode { get; private set; } = MailListMode.Threads;

    /// <summary>
    /// The flat messages currently loaded for <see cref="Mailbox"/> (and,
    /// within <see cref="MailMailbox.Inbox"/>, <see cref="Filter"/>), newest
    /// first. Loaded on every reload regardless of <see cref="ListMode"/> so
    /// <see cref="MailListMode.Flat"/> always has current data to show the
    /// moment <see cref="ToggleListMode"/> switches to it.
    /// </summary>
    public IReadOnlyList<MailMessage> Messages { get; private set; } = [];

    /// <summary>
    /// The thread rollups currently loaded for <see cref="Mailbox"/> (and,
    /// within <see cref="MailMailbox.Workspace"/>, <see cref="AgentFilter"/>),
    /// newest activity first. Loaded on every reload regardless of
    /// <see cref="ListMode"/>, mirroring <see cref="Messages"/>.
    /// </summary>
    public IReadOnlyList<MailThreadSummary> Threads { get; private set; } = [];

    /// <summary>
    /// The list pane's flattened, navigable rows for the current
    /// <see cref="ListMode"/>; see the class remarks.
    /// </summary>
    public IReadOnlyList<MailListRow> Rows { get; private set; } = [];

    /// <summary>
    /// The index of the selected row within <see cref="Rows"/>. Assigning
    /// this resolves <see cref="ViewMode"/>, <see cref="ThreadMessages"/>,
    /// and <see cref="SelectedMessage"/> for the newly-selected row; see the
    /// class remarks.
    /// </summary>
    public int SelectedRow
    {
        get => _selectedRow;
        set
        {
            _selectedRow = value;
            SyncSelectionBlocking();
        }
    }

    /// <summary>
    /// Which pane currently holds focus.
    /// </summary>
    public MailFocus Focus { get; set; } = MailFocus.List;

    /// <summary>
    /// What the detail pane currently renders.
    /// </summary>
    public MailViewMode ViewMode { get; private set; } = MailViewMode.Message;

    /// <summary>
    /// The selected row's thread, loaded when <see cref="ViewMode"/> is
    /// <see cref="MailViewMode.Thread"/>.
    /// </summary>
    public IReadOnlyList<MailMessage> ThreadMessages { get; private set; } = [];

    /// <summary>
    /// The message the detail pane and the u/a/r gestures act on: the
    /// selected row's own message when it is a <see cref="MailListRow.MessageRow"/>,
    /// or a thread row's most recent message otherwise (see the class
    /// remarks); null when no row is selected.
    /// </summary>
    public MailMessage? SelectedMessage { get; private set; }

    private int _selectedRow;
    private readonly HashSet<string> _expandedThreadIds = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IReadOnlyList<MailMessage>> _threadMessageCache =
        new(StringComparer.Ordinal);

    /// <summary>
    /// Thread ids, among <see cref="Threads"/>, that carry at least one
    /// message unread and addressed to <see cref="Actor"/> as a to or cc
    /// recipient - populated only within <see cref="MailMailbox.Workspace"/>,
    /// from the actor's own <see cref="MailDataLoader.LoadInboxThreadsAsync"/>
    /// rollups (never another agent's, per epic wi3 convention 8) run
    /// alongside the workspace query. <see cref="IsThreadUnreadToMe"/> is the
    /// public read of this set.
    /// </summary>
    private HashSet<string> _workspaceUnreadToMeThreadIds = new(StringComparer.Ordinal);

    /// <summary>
    /// Whether <paramref name="summary"/> should render with the
    /// unread-to-me highlight: outside <see cref="MailMailbox.Workspace"/>,
    /// <see cref="MailThreadSummary.UnreadCount"/> is already actor-scoped,
    /// so a positive count is enough; within Workspace, where
    /// <see cref="MailThreadSummary.UnreadCount"/> is always null (never
    /// exposing any agent's read state from the unscoped query), this reads
    /// the actor's own <see cref="_workspaceUnreadToMeThreadIds"/> instead -
    /// still only ever the actor's own state, never another agent's.
    /// </summary>
    public bool IsThreadUnreadToMe(MailThreadSummary summary)
        => Mailbox == MailMailbox.Workspace
            ? _workspaceUnreadToMeThreadIds.Contains(summary.ThreadId)
            : (summary.UnreadCount ?? 0) > 0;

    /// <summary>
    /// Reloads <see cref="Messages"/> and <see cref="Threads"/> for the
    /// current <see cref="Mailbox"/>, <see cref="Filter"/>, and
    /// <see cref="AgentFilter"/>, preserving the selected row (by thread or
    /// message id) when it is still present, or clamping to the reloaded
    /// list's bounds otherwise.
    /// </summary>
    public Task RefreshAsync(CancellationToken cancellationToken)
        => ReloadAsync(cancellationToken, resetToTop: false);

    /// <summary>
    /// Jumps to <paramref name="mailbox"/> when it differs from
    /// <see cref="Mailbox"/>: reloads <see cref="Messages"/> and
    /// <see cref="Threads"/> and resets <see cref="SelectedRow"/> to the top.
    /// A no-op when <paramref name="mailbox"/> is already active.
    /// </summary>
    public async Task SelectMailboxAsync(MailMailbox mailbox, CancellationToken cancellationToken)
    {
        if (Mailbox == mailbox)
        {
            return;
        }

        if (Mailbox == MailMailbox.Workspace)
        {
            AgentFilter = null;
        }

        Mailbox = mailbox;
        await ReloadAsync(cancellationToken, resetToTop: true).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets <see cref="AgentFilter"/> and reloads, resetting
    /// <see cref="SelectedRow"/> to the top the same way
    /// <see cref="SelectMailboxAsync"/> does. Meaningful only within
    /// <see cref="MailMailbox.Workspace"/>: <see cref="LoadMessagesAsync"/>
    /// and <see cref="LoadThreadsAsync"/> are the only places
    /// <see cref="AgentFilter"/> is read, so calling this while another
    /// mailbox is active reloads that mailbox's data unchanged.
    /// </summary>
    public Task SelectAgentFilterAsync(string? agent, CancellationToken cancellationToken)
    {
        AgentFilter = agent;
        return ReloadAsync(cancellationToken, resetToTop: true);
    }

    /// <summary>
    /// Changes <see cref="Filter"/> by <paramref name="delta"/> positions,
    /// cycling through the three <see cref="MailListFilter"/> values, and
    /// reloads. The cycle always advances <see cref="Filter"/> regardless of
    /// <see cref="Mailbox"/> or <see cref="ListMode"/>, but the filter only
    /// changes the reloaded messages within <see cref="MailMailbox.Inbox"/>
    /// and <see cref="MailListMode.Flat"/>; see <see cref="Filter"/>.
    /// </summary>
    public async Task CycleFilterAsync(int delta, CancellationToken cancellationToken)
    {
        var values = Enum.GetValues<MailListFilter>();
        var index = ((int)Filter + delta) % values.Length;

        if (index < 0)
        {
            index += values.Length;
        }

        Filter = values[index];
        await RefreshAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Toggles <see cref="ListMode"/> between <see cref="MailListMode.Threads"/>
    /// and <see cref="MailListMode.Flat"/> (Shift+V), rebuilding
    /// <see cref="Rows"/> from the already-loaded <see cref="Messages"/> and
    /// <see cref="Threads"/> - no reload. The selected row is preserved by
    /// identity when the same message or thread is present in both shapes,
    /// or clamped to the rebuilt list's bounds otherwise.
    /// </summary>
    public void ToggleListMode()
    {
        ListMode = ListMode == MailListMode.Threads ? MailListMode.Flat : MailListMode.Threads;
        RebuildRowsPreservingSelection();
    }

    /// <summary>
    /// Expands the thread's messages into indented rows immediately after
    /// it, fetching (and caching) them first when not already cached. A
    /// no-op when already expanded.
    /// </summary>
    public void ExpandThread(string threadId)
    {
        if (_expandedThreadIds.Add(threadId))
        {
            GetOrLoadThreadMessagesBlocking(threadId);
            RebuildRowsPreservingSelection();
        }
    }

    /// <summary>
    /// Collapses the thread's indented message rows. A no-op when already
    /// collapsed.
    /// </summary>
    public void CollapseThread(string threadId)
    {
        if (_expandedThreadIds.Remove(threadId))
        {
            RebuildRowsPreservingSelection();
        }
    }

    /// <summary>
    /// Expands the thread when collapsed, or collapses it when expanded (za).
    /// </summary>
    public void ToggleThreadFold(string threadId)
    {
        if (_expandedThreadIds.Contains(threadId))
        {
            CollapseThread(threadId);
        }
        else
        {
            ExpandThread(threadId);
        }
    }

    /// <summary>
    /// Expands every thread currently in <see cref="Threads"/> (zR).
    /// </summary>
    public void ExpandAllThreads()
    {
        foreach (var thread in Threads)
        {
            _expandedThreadIds.Add(thread.ThreadId);
            GetOrLoadThreadMessagesBlocking(thread.ThreadId);
        }

        RebuildRowsPreservingSelection();
    }

    /// <summary>
    /// Collapses every thread (zM).
    /// </summary>
    public void CollapseAllThreads()
    {
        _expandedThreadIds.Clear();
        RebuildRowsPreservingSelection();
    }

    /// <summary>
    /// Switches the detail pane to <see cref="MailViewMode.Thread"/>,
    /// loading the selected message's thread. Has no effect, and returns
    /// false, when no message is selected. A manual override of whatever
    /// <see cref="SelectedRow"/>'s assignment already resolved <see cref="ViewMode"/>
    /// to; the next selection change resolves it again from that row.
    /// </summary>
    public async Task<bool> ShowThreadAsync(CancellationToken cancellationToken)
    {
        if (SelectedMessage is not { } message)
        {
            return false;
        }

        ViewMode = MailViewMode.Thread;
        ThreadMessages = await loader.LoadThreadAsync(message.ThreadId, cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// Switches the detail pane back to <see cref="MailViewMode.Message"/>.
    /// See <see cref="ShowThreadAsync"/>'s remark on this being a manual,
    /// per-selection override.
    /// </summary>
    public void ShowMessage()
    {
        ViewMode = MailViewMode.Message;
        ThreadMessages = [];
    }

    /// <summary>
    /// Reloads <see cref="Messages"/>, <see cref="Threads"/>, and (within
    /// <see cref="MailMailbox.Workspace"/>) <see cref="_workspaceUnreadToMeThreadIds"/>,
    /// rebuilds <see cref="Rows"/>, and either resets <see cref="SelectedRow"/>
    /// to the top or restores it by row identity, per <paramref name="resetToTop"/>.
    /// Clears the thread-message cache first so an expanded thread's rows
    /// always reflect the fresh reload, not stale data from before it.
    /// </summary>
    private async Task ReloadAsync(CancellationToken cancellationToken, bool resetToTop)
    {
        var previousKey = !resetToTop && Rows.Count > 0 && _selectedRow >= 0 && _selectedRow < Rows.Count
            ? RowKey(Rows[_selectedRow])
            : null;

        Messages = await LoadMessagesAsync(cancellationToken).ConfigureAwait(false);
        Threads = await LoadThreadsAsync(cancellationToken).ConfigureAwait(false);

        _workspaceUnreadToMeThreadIds = Mailbox == MailMailbox.Workspace
            ? (await loader.LoadInboxThreadsAsync(Actor, cancellationToken).ConfigureAwait(false))
                .Where(t => (t.UnreadCount ?? 0) > 0)
                .Select(t => t.ThreadId)
                .ToHashSet(StringComparer.Ordinal)
            : [];

        _threadMessageCache.Clear();
        RebuildRows();

        var newIndex = previousKey is null ? -1 : IndexOfRow(Rows, previousKey);
        SelectedRow = newIndex >= 0
            ? newIndex
            : resetToTop
                ? 0
                : Math.Clamp(_selectedRow, 0, Math.Max(0, Rows.Count - 1));
    }

    /// <summary>
    /// Rebuilds <see cref="Rows"/> from the current <see cref="Messages"/> or
    /// <see cref="Threads"/> (per <see cref="ListMode"/>) and preserves the
    /// selected row by identity across the rebuild, or clamps when the
    /// previously-selected row is gone (for example a thread just collapsed
    /// out from under an indented child row that was selected).
    /// </summary>
    private void RebuildRowsPreservingSelection()
    {
        var previousKey = Rows.Count > 0 && _selectedRow >= 0 && _selectedRow < Rows.Count
            ? RowKey(Rows[_selectedRow])
            : null;

        RebuildRows();

        var newIndex = previousKey is null ? -1 : IndexOfRow(Rows, previousKey);
        SelectedRow = newIndex >= 0 ? newIndex : Math.Clamp(_selectedRow, 0, Math.Max(0, Rows.Count - 1));
    }

    private void RebuildRows()
    {
        if (ListMode == MailListMode.Flat)
        {
            Rows = Messages.Select(m => (MailListRow)new MailListRow.MessageRow(m, ThreadChild: false)).ToList();
            return;
        }

        var rows = new List<MailListRow>(Threads.Count);

        foreach (var thread in Threads)
        {
            var expanded = _expandedThreadIds.Contains(thread.ThreadId);
            rows.Add(new MailListRow.Thread(thread, expanded));

            if (expanded)
            {
                var children = GetOrLoadThreadMessagesBlocking(thread.ThreadId);
                rows.AddRange(children.Select(m => (MailListRow)new MailListRow.MessageRow(m, ThreadChild: true)));
            }
        }

        Rows = rows;
    }

    /// <summary>
    /// Resolves <see cref="ViewMode"/>, <see cref="ThreadMessages"/>, and
    /// <see cref="SelectedMessage"/> for whatever row <see cref="_selectedRow"/>
    /// now points at in <see cref="Rows"/>; see the class remarks. Blocking,
    /// like every other store-facing call in this TUI layer: called from
    /// <see cref="SelectedRow"/>'s setter, so it runs once per selection
    /// change rather than once per render.
    /// </summary>
    private void SyncSelectionBlocking()
    {
        var row = Rows.Count > 0 && _selectedRow >= 0 && _selectedRow < Rows.Count ? Rows[_selectedRow] : null;

        switch (row)
        {
            case MailListRow.Thread threadRow:
                var messages = GetOrLoadThreadMessagesBlocking(threadRow.Summary.ThreadId);
                ViewMode = MailViewMode.Thread;
                ThreadMessages = messages;
                SelectedMessage = messages.Count > 0 ? messages[^1] : null;
                break;

            case MailListRow.MessageRow messageRow:
                ViewMode = MailViewMode.Message;
                ThreadMessages = [];
                SelectedMessage = messageRow.Message;
                break;

            default:
                ViewMode = MailViewMode.Message;
                ThreadMessages = [];
                SelectedMessage = null;
                break;
        }
    }

    private IReadOnlyList<MailMessage> GetOrLoadThreadMessagesBlocking(string threadId)
    {
        if (_threadMessageCache.TryGetValue(threadId, out var cached))
        {
            return cached;
        }

        var messages = loader.LoadThreadAsync(threadId, CancellationToken.None).GetAwaiter().GetResult();
        _threadMessageCache[threadId] = messages;
        return messages;
    }

    /// <summary>
    /// A row's stable identity across a reload or a fold/list-mode change:
    /// its thread id for a thread row, or its message id for a message row -
    /// used to keep the same logical row selected across <see cref="Rows"/>
    /// being rebuilt.
    /// </summary>
    private static string RowKey(MailListRow row) => row switch
    {
        MailListRow.Thread t => $"thread:{t.Summary.ThreadId}",
        MailListRow.MessageRow m => $"message:{m.Message.Id}",
        _ => ""
    };

    private static int IndexOfRow(IReadOnlyList<MailListRow> rows, string key)
    {
        for (var i = 0; i < rows.Count; i++)
        {
            if (RowKey(rows[i]) == key)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Routes to the load method for <see cref="Mailbox"/>: Inbox is the
    /// only mailbox <see cref="Filter"/> affects, and Workspace is the only
    /// mailbox <see cref="AgentFilter"/> affects.
    /// </summary>
    private Task<IReadOnlyList<MailMessage>> LoadMessagesAsync(CancellationToken cancellationToken) => Mailbox switch
    {
        MailMailbox.Sent => loader.LoadSentAsync(Actor, cancellationToken),
        MailMailbox.All => loader.LoadAllAsync(Actor, cancellationToken),
        MailMailbox.Workspace => loader.LoadWorkspaceAsync(AgentFilter, cancellationToken),
        _ => loader.LoadInboxAsync(Actor, Filter, cancellationToken)
    };

    /// <summary>
    /// Routes to the thread-rollup load method for <see cref="Mailbox"/>,
    /// mirroring <see cref="LoadMessagesAsync"/> except that <see cref="Filter"/>
    /// never narrows the Inbox thread query; see <see cref="Filter"/>.
    /// </summary>
    private Task<IReadOnlyList<MailThreadSummary>> LoadThreadsAsync(CancellationToken cancellationToken) => Mailbox switch
    {
        MailMailbox.Sent => loader.LoadSentThreadsAsync(Actor, cancellationToken),
        MailMailbox.All => loader.LoadAllThreadsAsync(Actor, cancellationToken),
        MailMailbox.Workspace => loader.LoadWorkspaceThreadsAsync(AgentFilter, cancellationToken),
        _ => loader.LoadInboxThreadsAsync(Actor, cancellationToken)
    };
}
