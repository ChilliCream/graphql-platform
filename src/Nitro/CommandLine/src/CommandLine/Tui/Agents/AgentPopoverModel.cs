using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Shell;
using ChilliCream.Nitro.CommandLine.Tui.Widgets;
using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine.Tui.Agents;

/// <summary>
/// Which participation section a popover row or show-more row belongs to.
/// </summary>
internal enum AgentPopoverSection
{
    Mail,
    Tickets,
    Memory
}

/// <summary>
/// A tab-specific request payload carried by <see cref="PopoverResult.Request"/>, interpreted
/// by <see cref="AgentsMode.HandlePopoverRequest"/>.
/// </summary>
internal abstract record AgentPopoverRequest
{
    private AgentPopoverRequest()
    {
    }

    /// <summary>
    /// The delete confirmation should open for the agent named <paramref name="Name"/>,
    /// the same flow the Agents table's d key starts.
    /// </summary>
    public sealed record DeleteRequested(string Name) : AgentPopoverRequest;

    /// <summary>
    /// The popover's own agent's session id should be copied, the same flow the Agents
    /// table's y key starts, regardless of which row the table currently has selected.
    /// </summary>
    public sealed record CopyRequested(string Name, string? SessionId) : AgentPopoverRequest;

    /// <summary>
    /// A drilled-into item's id should be copied: a mail thread id, a ticket id, or a
    /// memory id.
    /// </summary>
    public sealed record CopyItemRequested(string Id) : AgentPopoverRequest;
}

/// <summary>
/// Loads one agent's identity plus its last mail, ticket, and memory participation, and
/// drives the centered detail overlay opened from the Agents tab: cursor navigation across
/// item and show-more rows, a nested full list per section, a nested read-only detail view
/// for any mail thread, ticket, or memory item, and the delete and copy-id gestures the
/// Agents table also exposes. Reloads on <see cref="Load"/> and recomputes presence and ages
/// on every <see cref="Tick"/>, both driven by the hosting shell.
/// </summary>
internal sealed class AgentPopoverModel : IPopover
{
    private const int SectionLimit = 10;
    private const int PanelChromeWidth = 4;
    private const int PanelChromeHeight = 2;

    /// <summary>
    /// Footer hints while the summary view is active.
    /// </summary>
    public static readonly IReadOnlyList<KeyHint> SummaryHints =
    [
        new KeyHint("j/k", "move"),
        new KeyHint("enter", "open"),
        new KeyHint("d", "delete"),
        new KeyHint("y", "copy id"),
        new KeyHint("esc", "close")
    ];

    /// <summary>
    /// Footer hints while a show-more list is active.
    /// </summary>
    public static readonly IReadOnlyList<KeyHint> ListHints =
    [
        new KeyHint("j/k", "move"),
        new KeyHint("enter", "open"),
        new KeyHint("esc", "back")
    ];

    /// <summary>
    /// Footer hints while a nested item detail view is active.
    /// </summary>
    public static readonly IReadOnlyList<KeyHint> DetailHints =
    [
        new KeyHint("j/k", "scroll"),
        new KeyHint("y", "copy id"),
        new KeyHint("esc", "back")
    ];

    private readonly string _agentName;
    private readonly IAgentStore _agentStore;
    private readonly IMailStore _mailStore;
    private readonly ITaskStore _taskStore;
    private readonly IMemoryStore _memoryStore;
    private readonly TimeProvider _timeProvider;

    private IReadOnlyList<MailThreadSummary> _mail = [];
    private IReadOnlyList<TaskItem> _tickets = [];
    private IReadOnlyList<MemoryParticipationEntry> _memory = [];
    private string _lastAgeSignature = string.Empty;
    private int _cursor;
    private AgentPopoverListMode? _listMode;
    private AgentPopoverSection? _listSection;
    private AgentItemDetailMode? _detailMode;

    public AgentPopoverModel(
        string agentName,
        IAgentStore agentStore,
        IMailStore mailStore,
        ITaskStore taskStore,
        IMemoryStore memoryStore,
        TimeProvider timeProvider)
    {
        ArgumentException.ThrowIfNullOrEmpty(agentName);
        ArgumentNullException.ThrowIfNull(agentStore);
        ArgumentNullException.ThrowIfNull(mailStore);
        ArgumentNullException.ThrowIfNull(taskStore);
        ArgumentNullException.ThrowIfNull(memoryStore);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _agentName = agentName;
        _agentStore = agentStore;
        _mailStore = mailStore;
        _taskStore = taskStore;
        _memoryStore = memoryStore;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// The agent's current row, or null only when it could not be found at all (the
    /// header then shows a not-found message instead of crashing).
    /// </summary>
    public AgentRow? Agent { get; private set; }

    /// <summary>
    /// The current footer hints: <see cref="DetailHints"/> while a nested item detail is
    /// open, <see cref="ListHints"/> while a show-more list is open, otherwise
    /// <see cref="SummaryHints"/>.
    /// </summary>
    public IReadOnlyList<KeyHint> Hints => _detailMode is not null
        ? DetailHints
        : _listMode is null ? SummaryHints : ListHints;

    /// <summary>
    /// Loads (or reloads) the agent row and its last <c>10</c> mail, ticket, and memory
    /// participation rows, blocking the caller. Keeps the cursor clamped to the reloaded
    /// row count.
    /// </summary>
    public void Load(CancellationToken cancellationToken = default) =>
        LoadAsync(cancellationToken).GetAwaiter().GetResult();

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var agent = await _agentStore.FindAsync(_agentName, cancellationToken);

        if (agent is not null)
        {
            Agent = agent;
        }

        _mail = await _mailStore.QueryParticipationThreadsAsync(_agentName, SectionLimit, cancellationToken);
        _tickets = await _taskStore.QueryParticipationAsync(_agentName, SectionLimit, cancellationToken);
        _memory = await _memoryStore.QueryParticipationAsync(_agentName, SectionLimit, cancellationToken);

        _cursor = Math.Clamp(_cursor, 0, Math.Max(0, TotalSelectableRows - 1));

        // An open show-more list holds its own snapshot of the section it lists, so a reload
        // (a DataChangedEvent) rebuilds it too, keeping its selected row.
        if (_listMode is { } openList && _listSection is { } section)
        {
            _listMode = BuildListMode(section, openList.Selected);
        }

        _lastAgeSignature = ComputeAgeSignature(_timeProvider.GetUtcNow());
    }

    /// <summary>
    /// Recomputes the agent's resolved state and formatted ages as of now. Returns
    /// whether anything the render depends on changed since the last call.
    /// </summary>
    public bool Tick()
    {
        var signature = ComputeAgeSignature(_timeProvider.GetUtcNow());

        if (signature == _lastAgeSignature)
        {
            return false;
        }

        _lastAgeSignature = signature;
        return true;
    }

    /// <summary>
    /// Builds a signature from every line currently visible: the show-more list's rows when
    /// one is open, otherwise the summary's resolved state plus its rendered lines.
    /// </summary>
    private string ComputeAgeSignature(DateTimeOffset now)
    {
        if (_listMode is { } listMode)
        {
            return string.Join('\u0001', listMode.FormatRows(now));
        }

        if (Agent is null)
        {
            return string.Empty;
        }

        var lines = AgentPopoverView.BuildLines(Agent, _mail, _tickets, _memory, now, int.MaxValue, Locate(_cursor)).Lines;

        return AgentStateResolver.Resolve(Agent, now) + '\u0001' + string.Join('\u0001', lines);
    }

    private int TotalSelectableRows => _mail.Count + 1 + _tickets.Count + 1 + _memory.Count + 1;

    /// <summary>
    /// Handles one raw key: while a nested item detail is open, keys scroll it, y reports
    /// copying its id, and Escape closes it back to wherever it was opened from; while a
    /// show-more list is open (and no detail is), keys route there; otherwise j/k and the
    /// arrows move the cursor, Enter opens the selected row's detail or full list, d and y
    /// report the same delete and copy gestures the Agents table exposes, and Escape closes
    /// the popover.
    /// </summary>
    public PopoverResult? HandleKey(ConsoleKeyInfo info)
    {
        if (_detailMode is not null)
        {
            return HandleDetailKey(info);
        }

        if (_listMode is { } listMode)
        {
            HandleListKey(listMode, info);
            return null;
        }

        switch (info.Key)
        {
            case ConsoleKey.J:
            case ConsoleKey.DownArrow:
                MoveCursor(1);
                return null;

            case ConsoleKey.K:
            case ConsoleKey.UpArrow:
                MoveCursor(-1);
                return null;

            case ConsoleKey.Enter:
                OpenSelectedRow();
                return null;

            case ConsoleKey.D when info.Modifiers == ConsoleModifiers.None:
                return new PopoverResult.Request(new AgentPopoverRequest.DeleteRequested(_agentName));

            case ConsoleKey.Y when info.Modifiers == ConsoleModifiers.None:
                return new PopoverResult.Request(
                    new AgentPopoverRequest.CopyRequested(Agent?.Name ?? _agentName, Agent?.SessionId));

            case ConsoleKey.Escape:
                return new PopoverResult.Closed();

            default:
                return null;
        }
    }

    /// <summary>
    /// Routes one raw key to the open nested item detail: j/k and the arrows scroll, y
    /// reports copying its id, and Escape closes it back to wherever it was opened from.
    /// </summary>
    private PopoverResult? HandleDetailKey(ConsoleKeyInfo info)
    {
        switch (info.Key)
        {
            case ConsoleKey.J:
            case ConsoleKey.DownArrow:
                _detailMode!.ScrollDown();
                return null;

            case ConsoleKey.K:
            case ConsoleKey.UpArrow:
                _detailMode!.ScrollUp();
                return null;

            case ConsoleKey.Y when info.Modifiers == ConsoleModifiers.None:
                return new PopoverResult.Request(new AgentPopoverRequest.CopyItemRequested(_detailMode!.CopyId));

            case ConsoleKey.Escape:
                _detailMode = null;
                return null;

            default:
                return null;
        }
    }

    /// <summary>
    /// Routes one raw key to the open show-more list: Escape pops back to the summary, and
    /// Enter opens the highlighted row's item detail.
    /// </summary>
    private void HandleListKey(AgentPopoverListMode listMode, ConsoleKeyInfo info)
    {
        switch (listMode.HandleKey(info))
        {
            case AgentPopoverListAction.Back:
                _listMode = null;
                _listSection = null;
                break;

            case AgentPopoverListAction.OpenSelected:
                _detailMode = BuildDetail(listMode.SelectedItem);
                break;
        }
    }

    private void MoveCursor(int delta)
    {
        var total = TotalSelectableRows;

        if (total > 0)
        {
            _cursor = Math.Clamp(_cursor + delta, 0, total - 1);
        }
    }

    /// <summary>
    /// Opens the show-more list for the cursor's section when it sits on the show-more row,
    /// otherwise opens the cursor's item as a nested detail view.
    /// </summary>
    private void OpenSelectedRow()
    {
        var location = Locate(_cursor);

        if (location.IsShowMore)
        {
            _listSection = location.Section;
            _listMode = BuildListMode(location.Section, selected: 0);
            return;
        }

        _detailMode = BuildDetail(ItemAt(location));
    }

    /// <summary>
    /// The raw participation entry the summary's <paramref name="location"/> points at, or
    /// null for a show-more row or an out-of-range index.
    /// </summary>
    private object? ItemAt((AgentPopoverSection Section, bool IsShowMore, int ItemIndex) location)
    {
        if (location.IsShowMore || location.ItemIndex < 0)
        {
            return null;
        }

        return location.Section switch
        {
            AgentPopoverSection.Mail when location.ItemIndex < _mail.Count => _mail[location.ItemIndex],
            AgentPopoverSection.Tickets when location.ItemIndex < _tickets.Count => _tickets[location.ItemIndex],
            AgentPopoverSection.Memory when location.ItemIndex < _memory.Count => _memory[location.ItemIndex],
            _ => null
        };
    }

    /// <summary>
    /// Builds the nested detail view for one mail, ticket, or memory participation entry,
    /// or null when <paramref name="item"/> is null or an unrecognized kind.
    /// </summary>
    private AgentItemDetailMode? BuildDetail(object? item) => item switch
    {
        MailThreadSummary mail => AgentItemDetailMode.ForThread(_mailStore, mail.ThreadId),
        TaskItem task => AgentItemDetailMode.ForTask(_taskStore, task.Id),
        MemoryParticipationEntry memory => AgentItemDetailMode.ForMemory(_memoryStore, memory.Kind, memory.Id),
        _ => null
    };

    private AgentPopoverListMode BuildListMode(AgentPopoverSection section, int selected) => section switch
    {
        AgentPopoverSection.Mail => BuildMailListMode(selected),
        AgentPopoverSection.Tickets => BuildTicketListMode(selected),
        _ => BuildMemoryListMode(selected)
    };

    /// <summary>
    /// Resolves which section and row (or show-more row) <paramref name="cursor"/> points
    /// at, given the current item counts.
    /// </summary>
    private (AgentPopoverSection Section, bool IsShowMore, int ItemIndex) Locate(int cursor)
    {
        var offset = 0;

        foreach (var (section, count) in Sections())
        {
            if (cursor < offset + count)
            {
                return (section, false, cursor - offset);
            }

            offset += count;

            if (cursor == offset)
            {
                return (section, true, -1);
            }

            offset += 1;
        }

        return (AgentPopoverSection.Memory, true, -1);
    }

    private IEnumerable<(AgentPopoverSection Section, int Count)> Sections()
    {
        yield return (AgentPopoverSection.Mail, _mail.Count);
        yield return (AgentPopoverSection.Tickets, _tickets.Count);
        yield return (AgentPopoverSection.Memory, _memory.Count);
    }

    private AgentPopoverListMode BuildMailListMode(int selected)
    {
        var items = _mailStore.QueryParticipationThreadsAsync(_agentName, null, CancellationToken.None)
            .GetAwaiter().GetResult();

        var rows = items
            .Select(item => (Func<DateTimeOffset, int, string>)(
                (now, width) => AgentPopoverView.FormatMailRow(item, now, width)))
            .ToList();

        return new AgentPopoverListMode("Mail", rows, [.. items.Cast<object>()], _timeProvider, selected);
    }

    private AgentPopoverListMode BuildTicketListMode(int selected)
    {
        var items = _taskStore.QueryParticipationAsync(_agentName, null, CancellationToken.None)
            .GetAwaiter().GetResult();

        var rows = items
            .Select(item => (Func<DateTimeOffset, int, string>)(
                (_, width) => AgentPopoverView.FormatTicketRow(item, width)))
            .ToList();

        return new AgentPopoverListMode("Tickets", rows, [.. items.Cast<object>()], _timeProvider, selected);
    }

    private AgentPopoverListMode BuildMemoryListMode(int selected)
    {
        var items = _memoryStore.QueryParticipationAsync(_agentName, null, CancellationToken.None)
            .GetAwaiter().GetResult();

        var rows = items
            .Select(item => (Func<DateTimeOffset, int, string>)(
                (now, width) => AgentPopoverView.FormatMemoryRow(item, now, width)))
            .ToList();

        return new AgentPopoverListMode("Memory", rows, [.. items.Cast<object>()], _timeProvider, selected);
    }

    /// <summary>
    /// Renders the nested item detail or the show-more list at full size when one is open,
    /// otherwise the centered summary overlay at about 80% of the given area.
    /// </summary>
    public IRenderable Render(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return new Markup(string.Empty);
        }

        if (_detailMode is { } detailMode)
        {
            return detailMode.Render(width, height);
        }

        if (_listMode is { } listMode)
        {
            return listMode.Render(width, height);
        }

        var panelWidth = Math.Max(1, Math.Min(width, (int)(width * 0.8)));
        var panelHeight = Math.Max(1, Math.Min(height, (int)(height * 0.8)));
        var contentWidth = Math.Max(0, panelWidth - PanelChromeWidth);
        var interiorHeight = Math.Max(0, panelHeight - PanelChromeHeight);

        var now = _timeProvider.GetUtcNow();
        var selected = Locate(_cursor);
        var built = AgentPopoverView.BuildLines(Agent, _mail, _tickets, _memory, now, contentWidth, selected);

        var viewport = new Viewport(built.Lines.Count, interiorHeight);
        viewport.EnsureVisible(built.SelectedLineIndex);
        var (start, count) = viewport.Slice();

        var visible = new List<string>(interiorHeight);

        for (var i = 0; i < count; i++)
        {
            visible.Add(built.Lines[start + i]);
        }

        while (visible.Count < interiorHeight)
        {
            visible.Add(string.Empty);
        }

        var panel = ColumnPane.RenderWithHeader(Agent?.Name ?? _agentName, visible, focused: true);
        panel.Width = panelWidth;
        panel.Height = panelHeight;

        return new Align(panel, HorizontalAlignment.Center, VerticalAlignment.Middle)
            .Width(width)
            .Height(height);
    }
}
