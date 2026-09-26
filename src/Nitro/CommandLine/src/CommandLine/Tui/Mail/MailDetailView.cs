using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Details;
using ChilliCream.Nitro.CommandLine.Tui.Theming;
using ChilliCream.Nitro.CommandLine.Tui.Widgets;
using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// Renders a mail thread's messages, oldest first, in a scrollable detail panel and
/// maintains its scroll position. Used by a host outside the Mail tab that has already
/// loaded a thread, such as the agent detail popover's mail drill-in.
/// </summary>
internal sealed class MailDetailView
{
    /// <summary>
    /// Border and padding columns the panel spends on either side of its
    /// content.
    /// </summary>
    private const int PanelChromeWidth = 4;

    /// <summary>
    /// Border rows the panel spends above and below its content; the
    /// header is drawn on the top border row.
    /// </summary>
    private const int PanelChromeHeight = 2;

    /// <summary>
    /// The maximum number of passes used to reserve viewport indicator rows.
    /// </summary>
    private const int MaxIndicatorSettlePasses = 3;

    /// <summary>
    /// The <see cref="RenderThread"/> default when no harness lookup is given, resolving
    /// every name to no attribution.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> s_emptyHarnesses =
        new Dictionary<string, string>();

    private readonly Viewport _bodyViewport = new(0, 0);

    /// <summary>
    /// Scrolls the body down one line.
    /// </summary>
    public void ScrollDown() => _bodyViewport.ScrollBy(1);

    /// <summary>
    /// Scrolls the body up one line.
    /// </summary>
    public void ScrollUp() => _bodyViewport.ScrollBy(-1);

    /// <summary>
    /// Renders a thread's messages, oldest first, with the thread's subject as the header
    /// and optional <see cref="ChilliCream.Nitro.CommandLine.Services.Workspace.AgentRow.Harness"/>
    /// attribution. A null lookup or an absent or empty harness entry adds no attribution.
    /// </summary>
    public IRenderable RenderThread(
        IReadOnlyList<MailMessage> messages,
        int width,
        int height,
        bool focused,
        IReadOnlyDictionary<string, string>? harnessesByName = null)
    {
        if (width <= 0 || height <= 0)
        {
            return new Markup(string.Empty);
        }

        var safeWidth = Math.Max(1, width);
        var interiorWidth = Math.Max(1, safeWidth - PanelChromeWidth);
        var interiorHeight = Math.Max(1, height - PanelChromeHeight);
        var harnesses = harnessesByName ?? s_emptyHarnesses;

        var lines = BuildThreadLines(messages, interiorWidth, harnesses);

        IRenderable content = lines.Count == 0
            ? Align.Center(new Markup(Markup.Escape("No messages.")), VerticalAlignment.Middle)
            : new Rows(RenderVisibleLines(lines, interiorHeight).Select(Row));

        var borderToken = focused ? "board.column.border.focused" : "board.column.border";

        return new Panel(content)
        {
            Header = new PanelHeader(BuildThreadHeader(messages)),
            Border = BoxBorder.Rounded,
            BorderStyle = ThemeTokens.GetStyle(borderToken),
            Width = safeWidth,
            Height = Math.Max(1, height)
        };
    }

    private static TaskDetailBodyLine PlainLine(string text) => new(text, IsMarkup: false);

    /// <summary>
    /// Returns escaped text styled as a detail section header.
    /// </summary>
    private static TaskDetailBodyLine SectionHeaderLine(string text)
    {
        var style = ThemeTokens.GetStyle("detail.section.header").ToMarkup();
        var escaped = Markup.Escape(text);
        var content = style.Length == 0 ? escaped : $"[{style}]{escaped}[/]";
        return new TaskDetailBodyLine(content, IsMarkup: true);
    }

    private static string BuildThreadHeader(IReadOnlyList<MailMessage> messages)
        => messages.Count > 0 ? $"Thread: {Markup.Escape(messages[0].Subject)}" : "Thread";

    /// <summary>
    /// Returns the name with a non-empty harness attribution in parentheses, or the
    /// name alone when no attribution is available. The returned text is unescaped.
    /// </summary>
    private static string AttributeHarness(string name, IReadOnlyDictionary<string, string> harnessesByName)
        => harnessesByName.TryGetValue(name, out var harness) && harness.Length > 0
            ? $"{name} ({harness})"
            : name;

    private static IReadOnlyList<TaskDetailBodyLine> BuildThreadLines(
        IReadOnlyList<MailMessage> messages, int width, IReadOnlyDictionary<string, string> harnessesByName)
    {
        if (messages.Count == 0)
        {
            return [];
        }

        var lines = new List<TaskDetailBodyLine>();

        for (var i = 0; i < messages.Count; i++)
        {
            if (i > 0)
            {
                lines.Add(PlainLine(string.Empty));
                lines.Add(PlainLine(new string('-', Math.Clamp(width, 1, 40))));
            }

            var message = messages[i];
            lines.Add(SectionHeaderLine(
                $"{AttributeHarness(message.Sender, harnessesByName)} - {FormatTimestamp(message.CreatedAt)}"));
            lines.AddRange(TaskDetailSections.WrapText(message.Body, width).Select(PlainLine));
        }

        return lines;
    }

    private static string FormatTimestamp(DateTimeOffset value)
        => value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm");

    /// <summary>
    /// Slices the body's visible window, reserving rows for "N more
    /// above/below" indicators once the lines no longer fit
    /// <paramref name="interiorHeight"/>, and padding the result with
    /// blank lines so the panel's border reaches the bottom.
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
    /// Wraps an escaped line as markup, rendering an empty line as a single space.
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
