using ChilliCream.Nitro.CommandLine.Tui.Theming;
using ChilliCream.Nitro.CommandLine.Tui.Widgets;
using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine.Tui.Details;

/// <summary>
/// Renders a <see cref="TaskDetailModel"/> as a metadata sidebar next to a
/// scrollable body of the task's long-text sections, dependencies, blocks,
/// and comments. Owns the body's scroll position; the model owns everything
/// else.
/// </summary>
internal sealed class TaskDetailView
{
    private const int MinSidebarWidth = 20;
    private const int MaxSidebarWidth = 40;

    private readonly TaskDetailModel _model;
    private readonly Viewport _bodyViewport = new(0, 0);
    private int _scrollAnchor;

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
    /// Renders the sidebar and body side by side into the given content
    /// area.
    /// </summary>
    public IRenderable Render(int width, int height, bool focused)
    {
        var sidebarWidth = Math.Clamp(width / 3, MinSidebarWidth, MaxSidebarWidth);
        sidebarWidth = Math.Min(sidebarWidth, Math.Max(1, width - 1));

        return new Layout("detail").SplitColumns(
            new Layout("sidebar", RenderSidebar(focused)).Size(sidebarWidth),
            new Layout("body", RenderBody(Math.Max(1, width - sidebarWidth), height, focused)));
    }

    private void Scroll(int delta)
    {
        var maxIndex = Math.Max(0, _bodyViewport.TotalCount - 1);
        _scrollAnchor = Math.Clamp(_scrollAnchor + delta, 0, maxIndex);
        _bodyViewport.EnsureVisible(_scrollAnchor);
    }

    private IRenderable RenderSidebar(bool focused)
    {
        var borderToken = focused ? "board.column.border.focused" : "board.column.border";

        IRenderable content = _model.Task is { } task
            ? new Rows(TaskDetailSidebar.Build(task, _model.Labels, _model.BlockedBy)
                .Select(line => (IRenderable)new Markup(line)))
            : new Markup(Markup.Escape(NoTaskMessage()));

        return new Panel(content)
        {
            Header = new PanelHeader("Details"),
            Border = BoxBorder.Rounded,
            BorderStyle = ThemeTokens.GetStyle(borderToken)
        };
    }

    private IRenderable RenderBody(int width, int height, bool focused)
    {
        var innerWidth = Math.Max(1, width - 4);
        var innerHeight = Math.Max(1, height - 2);

        var lines = TaskDetailBody.Build(_model, innerWidth, focused);
        _bodyViewport.Update(lines.Count, innerHeight);
        _scrollAnchor = Math.Clamp(_scrollAnchor, 0, Math.Max(0, lines.Count - 1));
        _bodyViewport.EnsureVisible(_scrollAnchor);

        var (start, count) = _bodyViewport.Slice();
        var visible = new List<IRenderable>(count);

        for (var i = start; i < start + count; i++)
        {
            var line = lines[i];
            visible.Add(new Markup(line.IsMarkup ? line.Content : Markup.Escape(line.Content)));
        }

        IRenderable content = visible.Count == 0
            ? new Markup(Markup.Escape(_model.Task is null ? NoTaskMessage() : "No details."))
            : new Rows(visible);

        var borderToken = focused ? "board.column.border.focused" : "board.column.border";
        var header = _model.Task is { } task ? Markup.Escape(task.Title) : "Detail";

        return new Panel(content)
        {
            Header = new PanelHeader(header),
            Border = BoxBorder.Rounded,
            BorderStyle = ThemeTokens.GetStyle(borderToken)
        };
    }

    private string NoTaskMessage()
        => _model.CurrentTaskId is { } id ? $"Task {id} not found." : "No task selected.";
}
