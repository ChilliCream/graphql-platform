using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tui.Input;
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
/// A terminal outcome of <see cref="AgentPopoverModel.HandleKey"/> that the hosting
/// <see cref="Shell.TuiShell"/> is expected to act on. A null return means the key was
/// consumed without a shell-level effect.
/// </summary>
internal abstract record AgentPopoverResult
{
    private AgentPopoverResult()
    {
    }

    /// <summary>
    /// The popover should be dismissed.
    /// </summary>
    public sealed record Closed : AgentPopoverResult;

    /// <summary>
    /// The delete confirmation should open for the agent named <paramref name="Name"/>,
    /// the same flow the Agents table's d key starts.
    /// </summary>
    public sealed record DeleteRequested(string Name) : AgentPopoverResult;

    /// <summary>
    /// The popover's own agent's session id should be copied, the same flow the Agents
    /// table's y key starts, regardless of which row the table currently has selected.
    /// </summary>
    public sealed record CopyRequested(string Name, string? SessionId) : AgentPopoverResult;
}

/// <summary>
/// Loads one agent's identity plus its last mail, ticket, and memory participation, and
/// drives the centered detail overlay opened from the Agents tab: cursor navigation across
/// item and show-more rows, a nested full list per section, and the delete and copy-id
/// gestures the Agents table also exposes. Reloads on <see cref="Load"/> and recomputes
/// presence and ages on every <see cref="Tick"/>, both driven by the hosting shell.
/// </summary>
internal sealed class AgentPopoverModel
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
        new KeyHint("enter", "show more"),
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
    /// The current footer hints: <see cref="ListHints"/> while a show-more list is open,
    /// otherwise <see cref="SummaryHints"/>.
    /// </summary>
    public IReadOnlyList<KeyHint> Hints => _listMode is null ? SummaryHints : ListHints;

    /// <summary>
    /// Loads (or reloads) the agent row and its last <c>10</c> mail, ticket, and memory
    /// participation rows, blocking the caller. Keeps the cursor clamped to the reloaded
    /// row count.
    /// </summary>
    public void Load() => LoadAsync(CancellationToken.None).GetAwaiter().GetResult();

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
    /// Handles one raw key: while a show-more list is open, keys route there and Escape
    /// pops back to the summary; otherwise j/k and the arrows move the cursor, Enter opens
    /// a show-more row's full list, d and y report the same delete and copy gestures the
    /// Agents table exposes, and Escape closes the popover.
    /// </summary>
    public AgentPopoverResult? HandleKey(ConsoleKeyInfo info)
    {
        if (_listMode is { } listMode)
        {
            if (listMode.HandleKey(info))
            {
                _listMode = null;
                _listSection = null;
            }

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
                OpenShowMoreIfSelected();
                return null;

            case ConsoleKey.D when info.Modifiers == ConsoleModifiers.None:
                return new AgentPopoverResult.DeleteRequested(_agentName);

            case ConsoleKey.Y when info.Modifiers == ConsoleModifiers.None:
                return new AgentPopoverResult.CopyRequested(Agent?.Name ?? _agentName, Agent?.SessionId);

            case ConsoleKey.Escape:
                return new AgentPopoverResult.Closed();

            default:
                return null;
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

    private void OpenShowMoreIfSelected()
    {
        var location = Locate(_cursor);

        if (!location.IsShowMore)
        {
            return;
        }

        _listSection = location.Section;
        _listMode = BuildListMode(location.Section, selected: 0);
    }

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

        return new AgentPopoverListMode(ListTitle("Mail"), rows, _timeProvider, selected);
    }

    private AgentPopoverListMode BuildTicketListMode(int selected)
    {
        var items = _taskStore.QueryParticipationAsync(_agentName, null, CancellationToken.None)
            .GetAwaiter().GetResult();

        var rows = items
            .Select(item => (Func<DateTimeOffset, int, string>)(
                (_, width) => AgentPopoverView.FormatTicketRow(item, width)))
            .ToList();

        return new AgentPopoverListMode(ListTitle("Tickets"), rows, _timeProvider, selected);
    }

    private AgentPopoverListMode BuildMemoryListMode(int selected)
    {
        var items = _memoryStore.QueryParticipationAsync(_agentName, null, CancellationToken.None)
            .GetAwaiter().GetResult();

        var rows = items
            .Select(item => (Func<DateTimeOffset, int, string>)(
                (now, width) => AgentPopoverView.FormatMemoryRow(item, now, width)))
            .ToList();

        return new AgentPopoverListMode(ListTitle("Memory"), rows, _timeProvider, selected);
    }

    private string ListTitle(string section) => $"{Agent?.Name ?? _agentName} — {section}";

    /// <summary>
    /// Renders the show-more list at full size when one is open, otherwise the centered
    /// summary overlay at about 80% of the given area.
    /// </summary>
    public IRenderable Render(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return new Markup(string.Empty);
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
