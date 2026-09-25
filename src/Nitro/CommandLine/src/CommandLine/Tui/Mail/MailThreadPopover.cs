using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tui.Details;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Shell;
using ChilliCream.Nitro.CommandLine.Tui.Theming;
using ChilliCream.Nitro.CommandLine.Tui.Widgets;
using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// A tab-specific request payload carried by <see cref="PopoverResult.Request"/>, interpreted
/// by <see cref="MailMode.HandlePopoverRequest"/>.
/// </summary>
internal abstract record MailThreadPopoverRequest
{
    private MailThreadPopoverRequest()
    {
    }

    /// <summary>
    /// The popover's own thread id should be copied, the same flow the Mail table's y key
    /// starts.
    /// </summary>
    public sealed record CopyRequested(string ThreadId) : MailThreadPopoverRequest;
}

/// <summary>
/// Loads one workspace thread's messages and drives the read-only detail overlay opened from
/// the Mail tab: a header of participants, message count, and ages, then every message oldest
/// first with its recipients and wrapped body. Never marks anything read. Reloads on
/// <see cref="Load"/> and recomputes ages on every <see cref="Tick"/>, both driven by the
/// hosting shell.
/// </summary>
internal sealed class MailThreadPopoverModel : IPopover
{
    private const int PanelChromeWidth = 4;
    private const int PanelChromeHeight = 2;

    /// <summary>
    /// The popover's footer hints.
    /// </summary>
    public static readonly IReadOnlyList<KeyHint> DefaultHints =
    [
        new KeyHint("up/down", "prev/next"),
        new KeyHint("pgup/pgdn", "scroll"),
        new KeyHint("y", "copy id"),
        new KeyHint("esc", "close")
    ];

    private readonly IMailStore _mailStore;
    private readonly IAgentStore _agentStore;
    private readonly TimeProvider _timeProvider;
    private readonly Func<int, MailThreadSummary?> _moveSelection;
    private readonly Viewport _bodyViewport = new(0, 0);

    private string _threadId;
    private IReadOnlyList<MailMessage> _messages = [];
    private IReadOnlyDictionary<string, AgentRow?> _agentsBySender = new Dictionary<string, AgentRow?>();
    private string _lastAgeSignature = string.Empty;

    public MailThreadPopoverModel(
        string threadId,
        IMailStore mailStore,
        IAgentStore agentStore,
        TimeProvider timeProvider,
        Func<int, MailThreadSummary?> moveSelection)
    {
        ArgumentException.ThrowIfNullOrEmpty(threadId);
        ArgumentNullException.ThrowIfNull(mailStore);
        ArgumentNullException.ThrowIfNull(agentStore);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(moveSelection);

        _threadId = threadId;
        _mailStore = mailStore;
        _agentStore = agentStore;
        _timeProvider = timeProvider;
        _moveSelection = moveSelection;
    }

    /// <inheritdoc />
    public IReadOnlyList<KeyHint> Hints => DefaultHints;

    /// <summary>
    /// Loads (or reloads) the thread's messages, oldest first, and resolves each distinct
    /// sender's agent row for harness attribution, blocking the caller.
    /// </summary>
    public void Load(CancellationToken cancellationToken = default) =>
        LoadAsync(cancellationToken).GetAwaiter().GetResult();

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        _messages = await _mailStore.GetThreadMessagesAsync(_threadId, cancellationToken);

        var agentsBySender = new Dictionary<string, AgentRow?>();

        foreach (var sender in _messages.Select(m => m.Sender).Distinct())
        {
            agentsBySender[sender] = await _agentStore.FindAsync(sender, cancellationToken);
        }

        _agentsBySender = agentsBySender;
        _lastAgeSignature = ComputeAgeSignature(_timeProvider.GetUtcNow());
    }

    /// <summary>
    /// Recomputes every message's and the header's formatted age as of now. Returns whether
    /// anything the render depends on changed since the last call.
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

    private string ComputeAgeSignature(DateTimeOffset now)
    {
        if (_messages.Count == 0)
        {
            return string.Empty;
        }

        var started = MailAges.Format(_messages[0].CreatedAt, now);
        var lastActivity = MailAges.Format(_messages[^1].CreatedAt, now);
        var perMessage = string.Join('\u0001', _messages.Select(m => MailAges.Format(m.CreatedAt, now)));

        return $"{started}\u0001{lastActivity}\u0001{perMessage}";
    }

    /// <summary>
    /// Handles one raw key: Up/Down and j/k move to the previous or next table row and reload
    /// onto it, PageUp/PageDown and Ctrl+U/Ctrl+D scroll the body, g/G jump to the first or last
    /// line, y reports copying the thread id, and Escape closes the popover.
    /// </summary>
    public PopoverResult? HandleKey(ConsoleKeyInfo info)
    {
        switch (info.Key)
        {
            case ConsoleKey.DownArrow:
            case ConsoleKey.J:
                return MoveSelection(1);

            case ConsoleKey.UpArrow:
            case ConsoleKey.K:
                return MoveSelection(-1);

            case ConsoleKey.PageDown:
            case ConsoleKey.D when info.Modifiers == ConsoleModifiers.Control:
                ScrollByPage(1);
                return null;

            case ConsoleKey.PageUp:
            case ConsoleKey.U when info.Modifiers == ConsoleModifiers.Control:
                ScrollByPage(-1);
                return null;

            case ConsoleKey.G when info.Modifiers == ConsoleModifiers.None:
                _bodyViewport.ScrollBy(int.MinValue / 2);
                return null;

            case ConsoleKey.G when info.Modifiers == ConsoleModifiers.Shift:
                _bodyViewport.ScrollBy(int.MaxValue / 2);
                return null;

            case ConsoleKey.Y when info.Modifiers == ConsoleModifiers.None:
                return new PopoverResult.Request(new MailThreadPopoverRequest.CopyRequested(_threadId));

            case ConsoleKey.Escape:
                return new PopoverResult.Closed();

            default:
                return null;
        }
    }

    /// <summary>
    /// Moves to the thread <paramref name="delta"/> steps away from the current one in the
    /// owning table, reloads onto it, and resets the body scroll to the top. Does nothing at
    /// the first or last thread.
    /// </summary>
    private PopoverResult? MoveSelection(int delta)
    {
        if (_moveSelection(delta) is not { } next)
        {
            return null;
        }

        _threadId = next.ThreadId;
        Load();
        _bodyViewport.ScrollBy(int.MinValue / 2);
        return null;
    }

    private void ScrollByPage(int direction) =>
        _bodyViewport.ScrollBy(direction * Math.Max(1, _bodyViewport.WindowHeight / 2));

    /// <summary>
    /// Renders the centered detail overlay at about 80% of the given area.
    /// </summary>
    public IRenderable Render(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return new Markup(string.Empty);
        }

        var panelWidth = Math.Max(1, Math.Min(width, (int)(width * 0.8)));
        var panelHeight = Math.Max(1, Math.Min(height, (int)(height * 0.8)));
        var contentWidth = Math.Max(0, panelWidth - PanelChromeWidth);
        var interiorHeight = Math.Max(0, panelHeight - PanelChromeHeight);

        var now = _timeProvider.GetUtcNow();
        var lines = MailThreadPopoverView.BuildLines(_messages, _agentsBySender, now, contentWidth);

        _bodyViewport.Update(lines.Count, interiorHeight);
        var (start, count) = _bodyViewport.Slice();

        var visible = new List<string>(interiorHeight);

        for (var i = 0; i < count; i++)
        {
            visible.Add(lines[start + i]);
        }

        while (visible.Count < interiorHeight)
        {
            visible.Add(string.Empty);
        }

        var header = MailThreadPopoverView.BuildHeader(_messages);
        var panel = ColumnPane.RenderWithHeader(header, visible, focused: true);
        panel.Width = panelWidth;
        panel.Height = panelHeight;

        return new Align(panel, HorizontalAlignment.Center, VerticalAlignment.Middle)
            .Width(width)
            .Height(height);
    }
}

/// <summary>
/// Pure formatting for the mail thread popover: the panel title, the participants and
/// activity header, and each message's speaker line, recipients, and wrapped body.
/// </summary>
internal static class MailThreadPopoverView
{
    /// <summary>
    /// A blank spacer row. A single space, not an empty string, so the panel's row renderer
    /// keeps it as a line instead of collapsing it away.
    /// </summary>
    private const string BlankLine = " ";

    /// <summary>
    /// The panel title: the thread's subject, or a placeholder when it has no messages.
    /// </summary>
    public static string BuildHeader(IReadOnlyList<MailMessage> messages) =>
        messages.Count > 0 ? Markup.Escape(messages[0].Subject) : "Thread";

    /// <summary>
    /// Builds every line of the popover: a leading blank line, the header block
    /// (Participants, Messages, Started, Last activity), a blank line, then each message
    /// oldest first with its speaker line, its To and Cc lines when present, and its wrapped
    /// body, with a blank line between messages.
    /// </summary>
    public static IReadOnlyList<string> BuildLines(
        IReadOnlyList<MailMessage> messages,
        IReadOnlyDictionary<string, AgentRow?> agentsBySender,
        DateTimeOffset now,
        int width)
    {
        var lines = new List<string>();

        if (messages.Count == 0)
        {
            lines.Add("No messages.");
            return lines;
        }

        lines.Add(BlankLine);
        AppendHeaderFields(lines, messages, now);
        lines.Add(BlankLine);

        for (var i = 0; i < messages.Count; i++)
        {
            if (i > 0)
            {
                lines.Add(BlankLine);
            }

            AppendMessage(lines, messages[i], agentsBySender, now, width);
        }

        return lines;
    }

    private static void AppendHeaderFields(
        List<string> lines, IReadOnlyList<MailMessage> messages, DateTimeOffset now)
    {
        var participants = messages
            .SelectMany(m => m.Recipients.Select(r => r.Name).Append(m.Sender))
            .Distinct()
            .OrderBy(name => name, StringComparer.Ordinal);

        lines.Add(FormatField("Participants", string.Join(", ", participants)));
        lines.Add(FormatField("Messages", messages.Count.ToString()));
        lines.Add(FormatField("Started", MailAges.Format(messages[0].CreatedAt, now)));
        lines.Add(FormatField("Last activity", MailAges.Format(messages[^1].CreatedAt, now)));
    }

    private static void AppendMessage(
        List<string> lines,
        MailMessage message,
        IReadOnlyDictionary<string, AgentRow?> agentsBySender,
        DateTimeOffset now,
        int width)
    {
        var titleStyle = ThemeTokens.GetStyle("detail.section.header").ToMarkup();
        lines.Add(Stylize(titleStyle, Markup.Escape(FormatSpeakerLine(message, agentsBySender, now))));

        var to = FormatRecipients(message, MailRecipientKinds.To);

        if (to.Length > 0)
        {
            lines.Add(FormatField("To", to));
        }

        var cc = FormatRecipients(message, MailRecipientKinds.Cc);

        if (cc.Length > 0)
        {
            lines.Add(FormatField("Cc", cc));
        }

        foreach (var bodyLine in TaskDetailSections.WrapText(message.Body, width))
        {
            var escaped = Markup.Escape(bodyLine);
            lines.Add(escaped.Length == 0 ? BlankLine : escaped);
        }
    }

    private static string FormatSpeakerLine(
        MailMessage message, IReadOnlyDictionary<string, AgentRow?> agentsBySender, DateTimeOffset now)
    {
        var agent = agentsBySender.GetValueOrDefault(message.Sender);
        var name = agent?.Harness is { Length: > 0 } harness
            ? $"{message.Sender} ({AgentHarnessDisplay.Name(harness)})"
            : message.Sender;

        return $"{name} · {MailAges.Format(message.CreatedAt, now)}";
    }

    private static string FormatRecipients(MailMessage message, string kind) => string.Join(
        ", ",
        message.Recipients.Where(r => r.Kind == kind).OrderBy(r => r.Ordinal).Select(r => r.Name));

    private static string FormatField(string label, string value) =>
        $"[dim]{Markup.Escape(label)}:[/] {Markup.Escape(value)}";

    private static string Stylize(string styleMarkup, string content) =>
        styleMarkup.Length == 0 ? content : $"[{styleMarkup}]{content}[/]";
}
