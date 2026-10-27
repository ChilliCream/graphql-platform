using ChilliCream.Nitro.CommandLine.Tui.Widgets;
using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine.Tui.Agents;

/// <summary>
/// A terminal outcome of <see cref="AgentPopoverListMode.HandleKey"/> that the hosting
/// <see cref="AgentPopoverModel"/> is expected to act on.
/// </summary>
internal enum AgentPopoverListAction
{
    /// <summary>
    /// The key was consumed without a host-level effect.
    /// </summary>
    None,

    /// <summary>
    /// Escape was pressed; the host should pop back to the summary popover.
    /// </summary>
    Back,

    /// <summary>
    /// Enter was pressed on a row; the host should open <see cref="AgentPopoverListMode.SelectedItem"/>.
    /// </summary>
    OpenSelected
}

/// <summary>
/// The full, unlimited list opened from one of the agent popover's show-more rows: the same
/// row format as the summary section, filling the whole overlay area and scrollable with
/// j/k or the arrows. Escape reports back to <see cref="AgentPopoverModel"/> to pop back to
/// the summary; Enter reports back to open the selected row's detail.
/// </summary>
internal sealed class AgentPopoverListMode
{
    private const int PanelChromeWidth = 4;
    private const int PanelChromeHeight = 2;
    private const string EmptyMessage = "Nothing here yet.";

    private readonly string _sectionName;
    private readonly IReadOnlyList<Func<DateTimeOffset, int, string>> _rows;
    private readonly IReadOnlyList<object> _items;
    private readonly TimeProvider _timeProvider;
    private readonly Viewport _viewport = new(0, 0);

    private int _selected;

    /// <summary>
    /// Builds a list for the <paramref name="sectionName"/> kind (for example "Mail") from
    /// <paramref name="rows"/>, one formatter per row taking the current time and the content
    /// width it should render at, and <paramref name="items"/>, the same rows' underlying
    /// participation entries in the same order. The panel title shows the kind alongside the
    /// total row count.
    /// </summary>
    public AgentPopoverListMode(
        string sectionName,
        IReadOnlyList<Func<DateTimeOffset, int, string>> rows,
        IReadOnlyList<object> items,
        TimeProvider timeProvider,
        int selected = 0)
    {
        ArgumentNullException.ThrowIfNull(sectionName);
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _sectionName = sectionName;
        _rows = rows;
        _items = items;
        _timeProvider = timeProvider;
        _selected = rows.Count == 0 ? 0 : Math.Clamp(selected, 0, rows.Count - 1);
    }

    /// <summary>
    /// The index of the currently highlighted row.
    /// </summary>
    public int Selected => _selected;

    /// <summary>
    /// The participation entry behind the currently highlighted row, or null when the list
    /// is empty.
    /// </summary>
    public object? SelectedItem => _selected >= 0 && _selected < _items.Count ? _items[_selected] : null;

    /// <summary>
    /// Handles one raw key: j/k and the arrows move the highlight, Enter reports opening the
    /// highlighted row's detail, and Escape reports popping back to the summary.
    /// </summary>
    public AgentPopoverListAction HandleKey(ConsoleKeyInfo info)
    {
        switch (info.Key)
        {
            case ConsoleKey.J:
            case ConsoleKey.DownArrow:
                if (_rows.Count > 0)
                {
                    _selected = Math.Min(_selected + 1, _rows.Count - 1);
                }

                return AgentPopoverListAction.None;

            case ConsoleKey.K:
            case ConsoleKey.UpArrow:
                if (_rows.Count > 0)
                {
                    _selected = Math.Max(_selected - 1, 0);
                }

                return AgentPopoverListAction.None;

            case ConsoleKey.Enter:
                return _rows.Count > 0 ? AgentPopoverListAction.OpenSelected : AgentPopoverListAction.None;

            case ConsoleKey.Escape:
                return AgentPopoverListAction.Back;

            default:
                return AgentPopoverListAction.None;
        }
    }

    /// <summary>
    /// Renders the list as a header panel filling the entire given area.
    /// </summary>
    public IRenderable Render(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return new Markup(string.Empty);
        }

        var contentWidth = Math.Max(0, width - PanelChromeWidth);
        var interiorHeight = Math.Max(0, height - PanelChromeHeight);
        var now = _timeProvider.GetUtcNow();

        _viewport.Update(_rows.Count, interiorHeight);
        _viewport.EnsureVisible(_selected);
        var (start, count) = _viewport.Slice();

        var lines = new List<string>(Math.Max(interiorHeight, 0));

        if (_rows.Count == 0)
        {
            lines.Add(DisplayWidth.Truncate(EmptyMessage, contentWidth));
        }

        for (var i = 0; i < count; i++)
        {
            var index = start + i;
            var line = _rows[index](now, contentWidth);

            lines.Add(index == _selected ? AgentPopoverView.Highlight(line) : line);
        }

        while (lines.Count < interiorHeight)
        {
            lines.Add(string.Empty);
        }

        var panel = ColumnPane.Render(_sectionName, _rows.Count, lines, focused: true);
        panel.Width = Math.Max(1, width);
        panel.Height = Math.Max(1, height);

        return panel;
    }

    /// <summary>
    /// Formats every row's full text as of <paramref name="now"/>, used to detect a row age
    /// change between ticks without going through a render pass.
    /// </summary>
    internal IEnumerable<string> FormatRows(DateTimeOffset now) => _rows.Select(row => row(now, int.MaxValue));
}
