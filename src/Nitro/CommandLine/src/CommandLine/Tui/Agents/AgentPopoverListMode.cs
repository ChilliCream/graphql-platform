using ChilliCream.Nitro.CommandLine.Tui.Widgets;
using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine.Tui.Agents;

/// <summary>
/// The full, unlimited list opened from one of the agent popover's show-more rows: the same
/// row format as the summary section, filling the whole overlay area and scrollable with
/// j/k or the arrows. Escape reports back to <see cref="AgentPopoverModel"/> to pop back to
/// the summary; every other key, including Enter, has no effect here.
/// </summary>
internal sealed class AgentPopoverListMode
{
    private const int PanelChromeWidth = 4;
    private const int PanelChromeHeight = 2;
    private const string EmptyMessage = "Nothing here yet.";

    private readonly string _title;
    private readonly IReadOnlyList<Func<int, string>> _rows;
    private readonly Viewport _viewport = new(0, 0);

    private int _selected;

    /// <summary>
    /// Builds a list titled <paramref name="title"/> from <paramref name="rows"/>, one
    /// formatter per row taking the content width it should render at.
    /// </summary>
    public AgentPopoverListMode(string title, IReadOnlyList<Func<int, string>> rows)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(rows);

        _title = title;
        _rows = rows;
    }

    /// <summary>
    /// Handles one raw key. Returns <see langword="true"/> when Escape was pressed and the
    /// host should pop back to the summary popover.
    /// </summary>
    public bool HandleKey(ConsoleKeyInfo info)
    {
        switch (info.Key)
        {
            case ConsoleKey.J:
            case ConsoleKey.DownArrow:
                if (_rows.Count > 0)
                {
                    _selected = Math.Min(_selected + 1, _rows.Count - 1);
                }

                return false;

            case ConsoleKey.K:
            case ConsoleKey.UpArrow:
                if (_rows.Count > 0)
                {
                    _selected = Math.Max(_selected - 1, 0);
                }

                return false;

            case ConsoleKey.Escape:
                return true;

            default:
                return false;
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
            var line = _rows[index](contentWidth);

            lines.Add(index == _selected ? AgentPopoverView.Highlight(line) : line);
        }

        while (lines.Count < interiorHeight)
        {
            lines.Add(string.Empty);
        }

        var panel = ColumnPane.RenderWithHeader(_title, lines, focused: true);
        panel.Width = Math.Max(1, width);
        panel.Height = Math.Max(1, height);

        return panel;
    }
}
