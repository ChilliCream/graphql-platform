using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Details;
using ChilliCream.Nitro.CommandLine.Tui.Theming;
using ChilliCream.Nitro.CommandLine.Tui.Widgets;
using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// Renders the mail board's detail pane: either the selected message's
/// header and body, or its whole thread in chronological order, as a
/// scrollable body inside a bordered panel. Owns the body's scroll
/// position; <see cref="MailState"/> owns everything else.
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
    /// The number of distinct above/below indicator combinations the
    /// body's viewport can settle on, bounding how many times reserving
    /// space for them needs to be recomputed.
    /// </summary>
    private const int MaxIndicatorSettlePasses = 3;

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
    /// Scrolls the body to its first line.
    /// </summary>
    public void ScrollToTop() => _bodyViewport.ScrollBy(int.MinValue / 2);

    /// <summary>
    /// Scrolls the body to its last line.
    /// </summary>
    public void ScrollToBottom() => _bodyViewport.ScrollBy(int.MaxValue / 2);

    /// <summary>
    /// Resets the body's scroll position to the top. Called whenever the
    /// pane's content changes: a different message is selected, the
    /// filter changes, or the view mode toggles.
    /// </summary>
    public void ResetScroll() => _bodyViewport.Update(0, 0);

    /// <summary>
    /// Renders <paramref name="state"/>'s detail pane: the selected
    /// message when its <see cref="MailState.ViewMode"/> is
    /// <see cref="MailViewMode.Message"/>, or the whole thread when it is
    /// <see cref="MailViewMode.Thread"/>.
    /// </summary>
    public IRenderable Render(MailState state, int width, int height, bool focused)
    {
        if (width <= 0 || height <= 0)
        {
            return new Markup(string.Empty);
        }

        var safeWidth = Math.Max(1, width);
        var interiorWidth = Math.Max(1, safeWidth - PanelChromeWidth);
        var interiorHeight = Math.Max(1, height - PanelChromeHeight);

        var lines = state.ViewMode == MailViewMode.Thread
            ? BuildThreadLines(state, interiorWidth)
            : BuildMessageLines(state, interiorWidth);

        IRenderable content = lines.Count == 0
            ? Align.Center(new Markup(Markup.Escape(NoMessageMessage(state))), VerticalAlignment.Middle)
            : new Rows(RenderVisibleLines(lines, interiorHeight).Select(Row));

        var borderToken = focused ? "board.column.border.focused" : "board.column.border";

        return new Panel(content)
        {
            Header = new PanelHeader(BuildHeader(state)),
            Border = BoxBorder.Rounded,
            BorderStyle = ThemeTokens.GetStyle(borderToken),
            Width = safeWidth,
            Height = Math.Max(1, height)
        };
    }

    private static string BuildHeader(MailState state)
    {
        if (state.ViewMode == MailViewMode.Thread)
        {
            return state.SelectedMessage is { } message
                ? $"Thread: {Markup.Escape(message.Subject)}"
                : "Thread";
        }

        return state.SelectedMessage is { } selected
            ? $"[dim]{Markup.Escape(selected.Id)}[/] {Markup.Escape(selected.Subject)}"
            : "Detail";
    }

    private static string NoMessageMessage(MailState state)
        => state.Messages.Count == 0 ? "No messages." : "No message selected.";

    private static IReadOnlyList<string> BuildMessageLines(MailState state, int width)
    {
        if (state.SelectedMessage is not { } message)
        {
            return [];
        }

        var lines = new List<string>
        {
            $"From: {message.Sender}",
            $"To: {string.Join(", ", RecipientNames(message, MailRecipientKinds.To))}"
        };

        var cc = RecipientNames(message, MailRecipientKinds.Cc);

        if (cc.Count > 0)
        {
            lines.Add($"Cc: {string.Join(", ", cc)}");
        }

        lines.Add($"Date: {FormatTimestamp(message.CreatedAt)}");
        lines.AddRange(BuildRecipientStateLines(message));
        lines.Add(string.Empty);
        lines.AddRange(TaskDetailSections.WrapText(message.Body, width));

        return lines;
    }

    /// <summary>
    /// One line per recipient, stating that recipient's own read/archived
    /// state and attributed by name: <c>"alice: read 2026-01-01 00:00"</c>
    /// or <c>"bob: unread"</c>, with <c>", archived"</c> appended where set.
    /// The attribution is the point - a reader must never mistake another
    /// agent's state for their own, so every row, including the actor's own
    /// when the actor is a recipient, renders through this same line with
    /// no second affordance. Empty for a message the actor sent, since
    /// <c>MailStore.BuildRecipients</c> never adds the sender to
    /// <see cref="MailMessage.Recipients"/> - there is no sender-side state
    /// to show and none is invented here.
    /// </summary>
    private static IReadOnlyList<string> BuildRecipientStateLines(MailMessage message)
        => message.Recipients
            .OrderBy(r => r.Ordinal)
            .Select(FormatRecipientState)
            .ToList();

    private static string FormatRecipientState(MailRecipient recipient)
    {
        var state = recipient.ReadAt is { } readAt
            ? $"read {FormatTimestamp(readAt)}"
            : "unread";

        return recipient.ArchivedAt is not null
            ? $"{recipient.Name}: {state}, archived"
            : $"{recipient.Name}: {state}";
    }

    private static IReadOnlyList<string> BuildThreadLines(MailState state, int width)
    {
        if (state.ThreadMessages.Count == 0)
        {
            return [];
        }

        var lines = new List<string>();

        for (var i = 0; i < state.ThreadMessages.Count; i++)
        {
            if (i > 0)
            {
                lines.Add(string.Empty);
                lines.Add(new string('-', Math.Clamp(width, 1, 40)));
            }

            var message = state.ThreadMessages[i];
            lines.Add($"{message.Sender} - {FormatTimestamp(message.CreatedAt)}");
            lines.AddRange(TaskDetailSections.WrapText(message.Body, width));
        }

        return lines;
    }

    private static IReadOnlyList<string> RecipientNames(MailMessage message, string kind)
        => message.Recipients
            .Where(r => r.Kind == kind)
            .OrderBy(r => r.Ordinal)
            .Select(r => r.Name)
            .ToList();

    private static string FormatTimestamp(DateTimeOffset value)
        => value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm");

    /// <summary>
    /// Slices the body's visible window, reserving rows for "N more
    /// above/below" indicators once the lines no longer fit
    /// <paramref name="interiorHeight"/>, and padding the result with
    /// blank lines so the panel's border reaches the bottom.
    /// </summary>
    private IReadOnlyList<string> RenderVisibleLines(IReadOnlyList<string> lines, int interiorHeight)
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
            visible.Add(Markup.Escape(lines[i]));
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
