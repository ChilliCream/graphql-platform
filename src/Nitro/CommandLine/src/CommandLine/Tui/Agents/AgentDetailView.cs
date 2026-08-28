using ChilliCream.Nitro.CommandLine.Tui.Details;
using ChilliCream.Nitro.CommandLine.Tui.Theming;
using ChilliCream.Nitro.CommandLine.Tui.Widgets;
using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine.Tui.Agents;

/// <summary>
/// Renders an <see cref="AgentDetailModel"/> as a scrollable body of its
/// Session, Identity, Tasks, and Sent mail sections inside a bordered panel,
/// the same single-panel shape <c>MailDetailView</c> uses. Owns the body's
/// scroll position; the model owns everything else.
/// </summary>
internal sealed class AgentDetailView
{
    /// <summary>
    /// Border and padding columns the panel spends on either side of its
    /// content.
    /// </summary>
    private const int PanelChromeWidth = 4;

    /// <summary>
    /// Border rows the panel spends above and below its content; the header
    /// is drawn on the top border row.
    /// </summary>
    private const int PanelChromeHeight = 2;

    /// <summary>
    /// The number of distinct above/below indicator combinations the body's
    /// viewport can settle on, bounding how many times reserving space for
    /// them needs to be recomputed.
    /// </summary>
    private const int MaxIndicatorSettlePasses = 3;

    private const string NoSessionMessage = "No session selected.";

    private readonly AgentDetailModel _model;
    private readonly TimeProvider _timeProvider;
    private readonly Viewport _bodyViewport = new(0, 0);

    public AgentDetailView(AgentDetailModel model, TimeProvider? timeProvider = null)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Scrolls the body down one line.
    /// </summary>
    public void ScrollDown() => _bodyViewport.ScrollBy(1);

    /// <summary>
    /// Scrolls the body up one line.
    /// </summary>
    public void ScrollUp() => _bodyViewport.ScrollBy(-1);

    /// <summary>
    /// Scrolls the body to its first line.
    /// </summary>
    public void ScrollToTop() => _bodyViewport.ScrollBy(int.MinValue / 2);

    /// <summary>
    /// Scrolls the body to its last line.
    /// </summary>
    public void ScrollToBottom() => _bodyViewport.ScrollBy(int.MaxValue / 2);

    /// <summary>
    /// Renders the loaded participant's sections into the given content
    /// area.
    /// </summary>
    public IRenderable Render(int width, int height, bool focused)
    {
        if (width <= 0 || height <= 0)
        {
            return new Markup(string.Empty);
        }

        var safeWidth = Math.Max(1, width);
        var interiorWidth = Math.Max(1, safeWidth - PanelChromeWidth);
        var interiorHeight = Math.Max(1, height - PanelChromeHeight);

        var now = _timeProvider.GetUtcNow();
        var lines = AgentDetailBody.Build(_model, now, interiorWidth);

        IRenderable content = lines.Count == 0
            ? Align.Center(new Markup(Markup.Escape(NoSessionMessage)), VerticalAlignment.Middle)
            : new Rows(RenderVisibleLines(lines, interiorHeight).Select(Row));

        var borderToken = focused ? "board.column.border.focused" : "board.column.border";

        return new Panel(content)
        {
            Header = new PanelHeader(BuildHeader()),
            Border = BoxBorder.Rounded,
            BorderStyle = ThemeTokens.GetStyle(borderToken),
            Width = safeWidth,
            Height = Math.Max(1, height)
        };
    }

    private string BuildHeader()
        => _model.Participant is { } participant
            ? Markup.Escape(participant.Session.AgentName ?? AgentParticipantRow.UnboundLabel)
            : "Session detail";

    /// <summary>
    /// Slices the body's visible window, reserving rows for "N more
    /// above/below" indicators once the lines no longer fit
    /// <paramref name="interiorHeight"/>, and padding the result with blank
    /// lines so the panel's border reaches the bottom.
    /// </summary>
    private IReadOnlyList<string> RenderVisibleLines(IReadOnlyList<TaskDetailBodyLine> lines, int interiorHeight)
    {
        var reservedRows = 0;

        for (var pass = 0; pass < MaxIndicatorSettlePasses; pass++)
        {
            var windowHeight = Math.Max(0, interiorHeight - reservedRows);
            _bodyViewport.Update(lines.Count, windowHeight);

            var needed = (_bodyViewport.HiddenAbove > 0 ? 1 : 0) + (_bodyViewport.HiddenBelow > 0 ? 1 : 0);

            if (needed == reservedRows)
            {
                break;
            }

            reservedRows = needed;
        }

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

    private static string FormatIndicator(int hiddenCount, string direction) => $"  {hiddenCount} more {direction}";
}
