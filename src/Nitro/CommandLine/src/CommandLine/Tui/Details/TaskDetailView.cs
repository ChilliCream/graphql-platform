using ChilliCream.Nitro.CommandLine.Tui.Theming;
using ChilliCream.Nitro.CommandLine.Tui.Widgets;
using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine.Tui.Details;

/// <summary>
/// Renders a <see cref="TaskDetailModel"/> as a scrollable body of the task's
/// long-text sections, dependencies, blocks, and comments, next to a metadata
/// sidebar. Owns the body's scroll position; the model owns everything else.
/// </summary>
internal sealed class TaskDetailView
{
    /// <summary>
    /// The sidebar's fixed width when shown beside the body.
    /// </summary>
    private const int SidebarWidth = 34;

    /// <summary>
    /// The total frame width below which the body and sidebar stack in a
    /// single column instead of sitting side by side.
    /// </summary>
    private const int StackedWidthThreshold = 100;

    /// <summary>
    /// Border and padding columns a panel spends on either side of its
    /// content, at a given panel width.
    /// </summary>
    private const int PanelChromeWidth = 4;

    /// <summary>
    /// Border rows a panel spends above and below its content; the header is
    /// drawn on the top border row.
    /// </summary>
    private const int PanelChromeHeight = 2;

    /// <summary>
    /// The number of distinct above/below indicator combinations the body's
    /// viewport can settle on, bounding how many times reserving space for
    /// them needs to be recomputed.
    /// </summary>
    private const int MaxIndicatorSettlePasses = 3;

    private readonly TaskDetailModel _model;
    private readonly Viewport _bodyViewport = new(0, 0);
    private int _lastSelectedRowIndex = -1;

    public TaskDetailView(TaskDetailModel model)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
    }

    /// <summary>
    /// Scrolls the body down one line.
    /// </summary>
    public void ScrollDown() => Scroll(1);

    /// <summary>
    /// Scrolls the body up one line.
    /// </summary>
    public void ScrollUp() => Scroll(-1);

    /// <summary>
    /// Scrolls the body down by half the last rendered body height.
    /// </summary>
    public void PageDown() => Scroll(Math.Max(1, _bodyViewport.WindowHeight / 2));

    /// <summary>
    /// Scrolls the body up by half the last rendered body height.
    /// </summary>
    public void PageUp() => Scroll(-Math.Max(1, _bodyViewport.WindowHeight / 2));

    /// <summary>
    /// Scrolls the body to its first line.
    /// </summary>
    public void ScrollToTop() => Scroll(int.MinValue / 2);

    /// <summary>
    /// Scrolls the body to its last line.
    /// </summary>
    public void ScrollToBottom() => Scroll(int.MaxValue / 2);

    /// <summary>
    /// Renders the body and sidebar into the given content area, always
    /// filling it completely: side by side when <paramref name="width"/> is
    /// at least <see cref="StackedWidthThreshold"/>, stacked with the body
    /// above the sidebar otherwise.
    /// </summary>
    public IRenderable Render(int width, int height, bool focused)
    {
        if (width <= 0 || height <= 0)
        {
            return new Markup(string.Empty);
        }

        return width < StackedWidthThreshold
            ? RenderStacked(width, height, focused)
            : RenderSideBySide(width, height, focused);
    }

    private void Scroll(int delta) => _bodyViewport.ScrollBy(delta);

    private IRenderable RenderSideBySide(int width, int height, bool focused)
    {
        var sidebarWidth = Math.Min(SidebarWidth, Math.Max(1, width - 1));
        var bodyWidth = Math.Max(1, width - sidebarWidth);

        return new Layout("detail").SplitColumns(
            new Layout("body", RenderBodyPanel(bodyWidth, height, focused)),
            new Layout("sidebar", RenderSidebarPanel(sidebarWidth, height, focused)).Size(sidebarWidth));
    }

    private IRenderable RenderStacked(int width, int height, bool focused)
    {
        const int minSidebarHeight = PanelChromeHeight + 1;
        var maxSidebarHeight = Math.Max(minSidebarHeight, height - minSidebarHeight);
        var desiredSidebarHeight = SidebarLineCount(width) + PanelChromeHeight;
        var sidebarHeight = Math.Clamp(desiredSidebarHeight, minSidebarHeight, maxSidebarHeight);
        var bodyHeight = Math.Max(1, height - sidebarHeight);

        return new Rows(
            RenderBodyPanel(width, bodyHeight, focused),
            RenderSidebarPanel(width, sidebarHeight, focused));
    }

    /// <summary>
    /// The number of rows the sidebar's content occupies once its lines wrap
    /// to the interior width the sidebar panel renders at <paramref name="width"/>,
    /// matching the wrapping <see cref="RenderSidebarPanel"/>'s underlying
    /// panel applies so the fixed panel height it is sized against never
    /// clips a wrapped line.
    /// </summary>
    private int SidebarLineCount(int width)
    {
        if (_model.Task is not { } task)
        {
            return 1;
        }

        var interiorWidth = Math.Max(1, width - PanelChromeWidth);

        return TaskDetailSidebar.Build(task, _model.Labels, _model.BlockedBy)
            .Sum(line => TaskDetailSections.WrapLine(Markup.Remove(line), interiorWidth).Count);
    }

    private Panel RenderSidebarPanel(int width, int height, bool focused)
    {
        var safeWidth = Math.Max(1, width);
        var interiorHeight = Math.Max(1, height - PanelChromeHeight);

        IRenderable content = _model.Task is { } task
            ? new Rows(PadToHeight(
                    TaskDetailSidebar.Build(task, _model.Labels, _model.BlockedBy), interiorHeight)
                .Select(Row))
            : new Markup(Markup.Escape(NoTaskMessage()));

        var borderToken = focused ? "board.column.border.focused" : "board.column.border";

        return new Panel(content)
        {
            Header = new PanelHeader("Details"),
            Border = BoxBorder.Rounded,
            BorderStyle = ThemeTokens.GetStyle(borderToken),
            Width = safeWidth,
            Height = Math.Max(1, height)
        };
    }

    private Panel RenderBodyPanel(int width, int height, bool focused)
    {
        var safeWidth = Math.Max(1, width);
        var interiorWidth = Math.Max(1, safeWidth - PanelChromeWidth);
        var interiorHeight = Math.Max(1, height - PanelChromeHeight);

        var lines = TaskDetailBody.Build(_model, interiorWidth, focused);

        IRenderable content = lines.Count == 0
            ? Align.Center(
                new Markup(Markup.Escape(_model.Task is null ? NoTaskMessage() : "No details.")),
                VerticalAlignment.Middle)
            : new Rows(RenderBodyLines(lines, interiorHeight).Select(Row));

        var borderToken = focused ? "board.column.border.focused" : "board.column.border";

        return new Panel(content)
        {
            Header = new PanelHeader(BuildBodyHeader()),
            Border = BoxBorder.Rounded,
            BorderStyle = ThemeTokens.GetStyle(borderToken),
            Width = safeWidth,
            Height = Math.Max(1, height)
        };
    }

    private string BuildBodyHeader()
        => _model.Task is { } task
            ? $"[dim]{Markup.Escape(task.Id)}[/] [bold]{Markup.Escape(task.Title)}[/]"
            : "Detail";

    /// <summary>
    /// Slices the body's visible window, reserving rows for "N more
    /// above/below" indicators once the lines no longer fit
    /// <paramref name="interiorHeight"/>, keeping the selected dependency or
    /// blocks row visible when the row selection changes, and padding the
    /// result with blank lines so the panel's border reaches the bottom.
    /// </summary>
    private IReadOnlyList<string> RenderBodyLines(IReadOnlyList<TaskDetailBodyLine> lines, int interiorHeight)
    {
        var selectedLineIndex = IndexOfSelectedRow(lines);
        var selectionChanged = _model.SelectedRowIndex != _lastSelectedRowIndex;
        var reservedRows = 0;

        for (var pass = 0; pass < MaxIndicatorSettlePasses; pass++)
        {
            var windowHeight = Math.Max(0, interiorHeight - reservedRows);
            _bodyViewport.Update(lines.Count, windowHeight);

            if (selectionChanged && selectedLineIndex >= 0)
            {
                _bodyViewport.EnsureVisible(selectedLineIndex);
            }

            var needed = (_bodyViewport.HiddenAbove > 0 ? 1 : 0) + (_bodyViewport.HiddenBelow > 0 ? 1 : 0);

            if (needed == reservedRows)
            {
                break;
            }

            reservedRows = needed;
        }

        _lastSelectedRowIndex = _model.SelectedRowIndex;

        var (start, count) = _bodyViewport.Slice();
        var visible = new List<string>(interiorHeight);

        if (_bodyViewport.HiddenAbove > 0)
        {
            visible.Add(FormatIndicator(_bodyViewport.HiddenAbove, "above"));
        }

        for (var i = start; i < start + count; i++)
        {
            var line = lines[i];
            visible.Add(line.IsMarkup ? line.Content : Markup.Escape(line.Content));
        }

        if (_bodyViewport.HiddenBelow > 0)
        {
            visible.Add(FormatIndicator(_bodyViewport.HiddenBelow, "below"));
        }

        return PadToHeight(visible, interiorHeight);
    }

    /// <summary>
    /// Wraps one already-escaped display line as markup. A blank line is
    /// rendered as a single space: <see cref="Panel"/> silently drops a
    /// literal empty content row instead of showing it blank.
    /// </summary>
    private static IRenderable Row(string line) => new Markup(line.Length == 0 ? " " : line);

    private static IReadOnlyList<string> PadToHeight(IReadOnlyList<string> lines, int height)
    {
        if (lines.Count >= height)
        {
            return lines;
        }

        var padded = new List<string>(height);
        padded.AddRange(lines);

        while (padded.Count < height)
        {
            padded.Add(string.Empty);
        }

        return padded;
    }

    private static int IndexOfSelectedRow(IReadOnlyList<TaskDetailBodyLine> lines)
    {
        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i].IsSelectedRow)
            {
                return i;
            }
        }

        return -1;
    }

    private static string FormatIndicator(int hiddenCount, string direction) => $"  {hiddenCount} more {direction}";

    private string NoTaskMessage()
        => _model.CurrentTaskId is { } id ? $"Task {id} not found." : "No task selected.";
}
