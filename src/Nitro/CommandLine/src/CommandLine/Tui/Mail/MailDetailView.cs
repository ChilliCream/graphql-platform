using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
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

    /// <summary>
    /// The <see cref="Render"/> default when no client lookup is given,
    /// resolving every name to no attribution.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> EmptyClients =
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
    /// <see cref="MailViewMode.Thread"/>. <paramref name="clientsByName"/>
    /// attributes each party's <see cref="AgentRecord.Client"/> next to its
    /// name (sender, and each recipient's own state line); a name absent
    /// from it, or mapped to an empty client, renders with no attribution
    /// at all. Null is treated as empty, so every existing caller keeps
    /// working unchanged.
    /// </summary>
    public IRenderable Render(
        MailState state,
        int width,
        int height,
        bool focused,
        IReadOnlyDictionary<string, string>? clientsByName = null)
    {
        if (width <= 0 || height <= 0)
        {
            return new Markup(string.Empty);
        }

        var safeWidth = Math.Max(1, width);
        var interiorWidth = Math.Max(1, safeWidth - PanelChromeWidth);
        var interiorHeight = Math.Max(1, height - PanelChromeHeight);
        var clients = clientsByName ?? EmptyClients;

        var lines = state.ViewMode == MailViewMode.Thread
            ? BuildThreadLines(state, interiorWidth, clients)
            : BuildMessageLines(state, interiorWidth, clients);

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

    private static TaskDetailBodyLine PlainLine(string text) => new(text, IsMarkup: false);

    /// <summary>
    /// A styled header line via the <c>detail.section.header</c> token, the
    /// same token <see cref="ChilliCream.Nitro.CommandLine.Tui.Agents.AgentDetailBody"/>
    /// and <see cref="Details.TaskDetailBody"/> use for their section
    /// headers: the per-thread-message <c>"sender - date"</c> line here.
    /// </summary>
    private static TaskDetailBodyLine SectionHeaderLine(string text)
    {
        var style = ThemeTokens.GetStyle("detail.section.header").ToMarkup();
        var escaped = Markup.Escape(text);
        var content = style.Length == 0 ? escaped : $"[{style}]{escaped}[/]";
        return new TaskDetailBodyLine(content, IsMarkup: true);
    }

    /// <summary>
    /// A <c>"Label: value"</c> line with just the label styled via the
    /// <c>detail.section.header</c> token, for the message view's From/To/Cc/Date
    /// fields.
    /// </summary>
    private static TaskDetailBodyLine FieldLine(string label, string value)
    {
        var style = ThemeTokens.GetStyle("detail.section.header").ToMarkup();
        var labelText = $"{label}:";
        var labelMarkup = style.Length == 0 ? labelText : $"[{style}]{labelText}[/]";
        return new TaskDetailBodyLine($"{labelMarkup} {Markup.Escape(value)}", IsMarkup: true);
    }

    private static string BuildHeader(MailState state)
    {
        if (state.ViewMode == MailViewMode.Thread)
        {
            // ThreadMessages, not SelectedMessage: a collapsed thread row's
            // ViewMode defaults to Thread the moment it is selected (see
            // MailState's class remarks), and every message in a thread
            // shares one subject (ReplyMessageAsync inherits it from the
            // root), so this is equivalent to the old SelectedMessage-based
            // header whenever a message row set it, and correct for a
            // thread-row selection too.
            return state.ThreadMessages.Count > 0
                ? $"Thread: {Markup.Escape(state.ThreadMessages[0].Subject)}"
                : "Thread";
        }

        return state.SelectedMessage is { } selected
            ? $"[dim]{Markup.Escape(selected.Id)}[/] {Markup.Escape(selected.Subject)}"
            : "Detail";
    }

    private static string NoMessageMessage(MailState state)
        => state.Messages.Count == 0 ? "No messages." : "No message selected.";

    private static IReadOnlyList<TaskDetailBodyLine> BuildMessageLines(
        MailState state, int width, IReadOnlyDictionary<string, string> clientsByName)
    {
        if (state.SelectedMessage is not { } message)
        {
            return [];
        }

        var lines = new List<TaskDetailBodyLine>
        {
            FieldLine("From", AttributeClient(message.Sender, clientsByName)),
            FieldLine("To", string.Join(", ", RecipientNames(message, MailRecipientKinds.To)))
        };

        var cc = RecipientNames(message, MailRecipientKinds.Cc);

        if (cc.Count > 0)
        {
            lines.Add(FieldLine("Cc", string.Join(", ", cc)));
        }

        lines.Add(FieldLine("Date", FormatTimestamp(message.CreatedAt)));
        lines.AddRange(BuildRecipientStateLines(message, clientsByName));
        lines.Add(PlainLine(string.Empty));
        lines.AddRange(TaskDetailSections.WrapText(message.Body, width).Select(PlainLine));

        return lines;
    }

    /// <summary>
    /// <paramref name="name"/> suffixed with its <see cref="AgentRecord.Client"/>
    /// in parentheses when <paramref name="clientsByName"/> has a non-empty
    /// entry for it, or <paramref name="name"/> unchanged otherwise - an
    /// unknown name and a known name with an empty client render identically,
    /// per the epic's "empty means nothing shown, not a placeholder" rule.
    /// Deliberately returns raw text, not markup: both values are
    /// agent-supplied and may contain <c>[...]</c>. Every caller building a
    /// <see cref="TaskDetailBodyLine"/> from this text escapes it with
    /// <see cref="Markup.Escape(string)"/> itself before wrapping it in
    /// style markup, since it lands inside a line already carrying markup
    /// and can no longer rely on <see cref="RenderVisibleLines"/>'s
    /// plain-line escaping.
    /// </summary>
    private static string AttributeClient(string name, IReadOnlyDictionary<string, string> clientsByName)
        => clientsByName.TryGetValue(name, out var client) && client.Length > 0
            ? $"{name} ({client})"
            : name;

    /// <summary>
    /// One line per recipient, stating that recipient's own read/archived
    /// state and attributed by name: <c>"alice: read 2026-01-01 00:00"</c>
    /// or <c>"bob: unread"</c>, with <c>", archived"</c> appended where set.
    /// Styled via <c>mail.detail.recipient.unread</c> or
    /// <c>mail.detail.recipient.read</c> so a still-unread recipient stands
    /// out from one who has read it. The attribution is the point - a
    /// reader must never mistake another agent's state for their own, so
    /// every row, including the actor's own when the actor is a recipient,
    /// renders through this same line with no second affordance. Empty for
    /// a message the actor sent, since <c>MailStore.BuildRecipients</c>
    /// never adds the sender to <see cref="MailMessage.Recipients"/> -
    /// there is no sender-side state to show and none is invented here.
    /// </summary>
    private static IReadOnlyList<TaskDetailBodyLine> BuildRecipientStateLines(
        MailMessage message, IReadOnlyDictionary<string, string> clientsByName)
        => message.Recipients
            .OrderBy(r => r.Ordinal)
            .Select(r => FormatRecipientState(r, clientsByName))
            .ToList();

    private static TaskDetailBodyLine FormatRecipientState(
        MailRecipient recipient, IReadOnlyDictionary<string, string> clientsByName)
    {
        var unread = recipient.ReadAt is null;
        var state = recipient.ReadAt is { } readAt
            ? $"read {FormatTimestamp(readAt)}"
            : "unread";

        var label = AttributeClient(recipient.Name, clientsByName);
        var text = recipient.ArchivedAt is not null
            ? $"{label}: {state}, archived"
            : $"{label}: {state}";

        var token = unread ? "mail.detail.recipient.unread" : "mail.detail.recipient.read";
        var style = ThemeTokens.GetStyle(token).ToMarkup();
        var escaped = Markup.Escape(text);
        var content = style.Length == 0 ? escaped : $"[{style}]{escaped}[/]";

        return new TaskDetailBodyLine(content, IsMarkup: true);
    }

    private static IReadOnlyList<TaskDetailBodyLine> BuildThreadLines(
        MailState state, int width, IReadOnlyDictionary<string, string> clientsByName)
    {
        if (state.ThreadMessages.Count == 0)
        {
            return [];
        }

        var lines = new List<TaskDetailBodyLine>();

        for (var i = 0; i < state.ThreadMessages.Count; i++)
        {
            if (i > 0)
            {
                lines.Add(PlainLine(string.Empty));
                lines.Add(PlainLine(new string('-', Math.Clamp(width, 1, 40))));
            }

            var message = state.ThreadMessages[i];
            lines.Add(SectionHeaderLine(
                $"{AttributeClient(message.Sender, clientsByName)} - {FormatTimestamp(message.CreatedAt)}"));
            lines.AddRange(TaskDetailSections.WrapText(message.Body, width).Select(PlainLine));
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
