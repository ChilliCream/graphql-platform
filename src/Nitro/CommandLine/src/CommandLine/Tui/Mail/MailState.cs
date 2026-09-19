using ChilliCream.Nitro.CommandLine.Services.Mail;

namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// The mail board's loaded messages and threads, mailbox filters, selection,
/// focus, and detail view mode.
/// </summary>
internal sealed class MailState(string? actor, MailDataLoader loader)
{
    /// <summary>
    /// The acting agent, or null when the board has no identity.
    /// </summary>
    public string? Actor { get; } = actor;

    /// <summary>
    /// The acting agent required by personal mailbox queries; throws when no identity
    /// is available.
    /// </summary>
    private string RequiredActor
        => Actor ?? throw new InvalidOperationException("The board has no agent identity.");

    /// <summary>
    /// The selected mailbox, initially <see cref="MailMailbox.Workspace"/>.
    /// </summary>
    public MailMailbox Mailbox { get; private set; } = MailMailbox.Workspace;

    /// <summary>
    /// The read-state filter for Inbox messages and thread summaries.
    /// Other mailboxes ignore this filter.
    /// </summary>
    public MailListFilter Filter { get; private set; } = MailListFilter.Inbox;

    /// <summary>
    /// The agent whose sent or received mail is shown in Workspace, or null for all
    /// agents. Leaving Workspace clears the filter.
    /// </summary>
    public string? AgentFilter { get; private set; }

    /// <summary>
    /// The list shape, initially <see cref="MailListMode.Threads"/>.
    /// </summary>
    public MailListMode ListMode { get; private set; } = MailListMode.Threads;

    /// <summary>
    /// Messages loaded for the current mailbox and filters, newest first, regardless
    /// of the active list shape.
    /// </summary>
    public IReadOnlyList<MailMessage> Messages { get; private set; } = [];

    /// <summary>
    /// Thread summaries loaded for the current mailbox and filters, newest activity
    /// first, regardless of the active list shape.
    /// </summary>
    public IReadOnlyList<MailThreadSummary> Threads { get; private set; } = [];

    /// <summary>
    /// The navigable message rows, or thread rows followed by any expanded messages.
    /// </summary>
    public IReadOnlyList<MailListRow> Rows { get; private set; } = [];

    /// <summary>
    /// The selected index in <see cref="Rows"/>. Selecting a different row identity
    /// resolves its detail content and view mode; selecting the same identity preserves
    /// a manual view-mode override.
    /// </summary>
    public int SelectedRow
    {
        get => _selectedRow;
        set
        {
            _selectedRow = value;
            var key = _selectedRow >= 0 && _selectedRow < Rows.Count ? RowKey(Rows[_selectedRow]) : null;

            if (key == _syncedRowKey)
            {
                return;
            }

            _syncedRowKey = key;
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
    /// The selected message row's message or the selected thread's latest message,
    /// or null when no message is selected.
    /// </summary>
    public MailMessage? SelectedMessage { get; private set; }

    private int _selectedRow;

    /// <summary>
    /// The last synchronized row identity, or null when none is selected.
    /// </summary>
    private string? _syncedRowKey;

    private readonly HashSet<string> _expandedThreadIds = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IReadOnlyList<MailMessage>> _threadMessageCache =
        new(StringComparer.Ordinal);

    /// <summary>
    /// Workspace thread ids with unread mail addressed to the acting agent,
    /// loaded from that agent's inbox thread summaries.
    /// </summary>
    private HashSet<string> _workspaceUnreadToMeThreadIds = new(StringComparer.Ordinal);

    /// <summary>
    /// Whether the thread has unread mail for the acting agent.
    /// Workspace uses the separately loaded actor-specific thread ids.
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
    /// <see cref="MailMailbox.Workspace"/>.
    /// </summary>
    public Task SelectAgentFilterAsync(string? agent, CancellationToken cancellationToken)
    {
        AgentFilter = agent;
        return ReloadAsync(cancellationToken, resetToTop: true);
    }

    /// <summary>
    /// Cycles the inbox filter by the supplied number of positions and reloads.
    /// The filter affects both messages and threads in Inbox only.
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
    /// Toggles the list shape using loaded messages and thread summaries, preserving
    /// the selected identity when present or clamping its index otherwise.
    /// Resolving the selection may load the selected thread's messages.
    /// </summary>
    public void ToggleListMode()
    {
        ListMode = ListMode == MailListMode.Threads ? MailListMode.Flat : MailListMode.Threads;
        RebuildRowsPreservingSelection();
    }

    /// <summary>
    /// Expands the thread's message rows, loading its messages when needed.
    /// An already expanded thread is unchanged.
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
    /// Loads the selected message's thread and displays it until the selected row
    /// identity changes or the view is changed manually. Returns false without
    /// changing the view when no message is selected.
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
    /// Displays the selected message alone until the selected row identity changes
    /// or the view is changed manually.
    /// </summary>
    public void ShowMessage()
    {
        ViewMode = MailViewMode.Message;
        ThreadMessages = [];
    }

    /// <summary>
    /// Reloads mailbox data and rebuilds the rows, resetting to the first row when
    /// requested or retaining the selection when present. A retained selection gets
    /// fresh content while preserving its detail view mode.
    /// </summary>
    private async Task ReloadAsync(CancellationToken cancellationToken, bool resetToTop)
    {
        var previousKey = !resetToTop && Rows.Count > 0 && _selectedRow >= 0 && _selectedRow < Rows.Count
            ? RowKey(Rows[_selectedRow])
            : null;

        Messages = await LoadMessagesAsync(cancellationToken).ConfigureAwait(false);
        Threads = await LoadThreadsAsync(cancellationToken).ConfigureAwait(false);

        _workspaceUnreadToMeThreadIds = Mailbox == MailMailbox.Workspace && Actor is not null
            ? (await loader.LoadInboxThreadsAsync(Actor, MailListFilter.Inbox, cancellationToken).ConfigureAwait(false))
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

        if (newIndex >= 0)
        {
            RefreshSelectedRowContent();
        }
    }

    /// <summary>
    /// Refreshes the selected message and any displayed thread without changing the
    /// detail view mode.
    /// </summary>
    private void RefreshSelectedRowContent()
    {
        if (_selectedRow < 0 || _selectedRow >= Rows.Count)
        {
            return;
        }

        switch (Rows[_selectedRow])
        {
            case MailListRow.Thread threadRow:
                var threadMessages = GetOrLoadThreadMessagesBlocking(threadRow.Summary.ThreadId);
                SelectedMessage = threadMessages.Count > 0 ? threadMessages[^1] : null;

                if (ViewMode == MailViewMode.Thread)
                {
                    ThreadMessages = threadMessages;
                }

                break;

            case MailListRow.MessageRow messageRow:
                SelectedMessage = messageRow.Message;

                if (ViewMode == MailViewMode.Thread)
                {
                    ThreadMessages = GetOrLoadThreadMessagesBlocking(messageRow.Message.ThreadId);
                }

                break;
        }
    }

    /// <summary>
    /// Rebuilds <see cref="Rows"/> from the current <see cref="Messages"/> or
    /// <see cref="Threads"/> (per <see cref="ListMode"/>) and preserves the
    /// selected row by identity across the rebuild, or clamps when the
    /// previously-selected row is gone.
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
    /// now points at in <see cref="Rows"/>. Called from <see cref="SelectedRow"/>'s setter.
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
    /// A row identity containing its kind and its thread or message id.
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
        MailMailbox.Sent => loader.LoadSentAsync(RequiredActor, cancellationToken),
        MailMailbox.All => loader.LoadAllAsync(RequiredActor, cancellationToken),
        MailMailbox.Workspace => loader.LoadWorkspaceAsync(AgentFilter, cancellationToken),
        _ => loader.LoadInboxAsync(RequiredActor, Filter, cancellationToken)
    };

    /// <summary>
    /// Loads thread summaries for the current mailbox and filters.
    /// </summary>
    private async Task<IReadOnlyList<MailThreadSummary>> LoadThreadsAsync(CancellationToken cancellationToken)
    {
        switch (Mailbox)
        {
            case MailMailbox.Sent:
                return await loader.LoadSentThreadsAsync(RequiredActor, cancellationToken).ConfigureAwait(false);

            case MailMailbox.All:
                return await loader.LoadAllThreadsAsync(RequiredActor, cancellationToken).ConfigureAwait(false);

            case MailMailbox.Workspace:
                return await loader.LoadWorkspaceThreadsAsync(AgentFilter, cancellationToken).ConfigureAwait(false);

            default:
                var threads = await loader.LoadInboxThreadsAsync(RequiredActor, Filter, cancellationToken).ConfigureAwait(false);
                return Filter == MailListFilter.Unread
                    ? threads.Where(t => (t.UnreadCount ?? 0) > 0).ToList()
                    : threads;
        }
    }
}
