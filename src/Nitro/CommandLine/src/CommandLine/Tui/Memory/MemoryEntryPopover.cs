using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Tui.Agents;
using ChilliCream.Nitro.CommandLine.Tui.Details;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Shell;
using ChilliCream.Nitro.CommandLine.Tui.Widgets;
using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine.Tui.Memory;

/// <summary>
/// A tab-specific request payload carried by <see cref="PopoverResult.Request"/>, interpreted
/// by <see cref="MemoryMode.HandlePopoverRequest"/>.
/// </summary>
internal abstract record MemoryEntryPopoverRequest
{
    private MemoryEntryPopoverRequest()
    {
    }

    /// <summary>
    /// The popover's own entry id should be copied, the same flow the Memory table's y key
    /// starts.
    /// </summary>
    public sealed record CopyRequested(string EntryId) : MemoryEntryPopoverRequest;
}

/// <summary>
/// Loads one curated memory or journal entry and drives the read-only detail overlay opened
/// from the Memory tab: an id title, a header block, and the wrapped body. Reloads on
/// <see cref="Load"/> and recomputes ages on every <see cref="Tick"/>, both driven by the
/// hosting shell.
/// </summary>
internal sealed class MemoryEntryPopoverModel : IPopover
{
    private const int PanelChromeWidth = 4;
    private const int PanelChromeHeight = 2;

    /// <summary>
    /// The popover's footer hints.
    /// </summary>
    public static readonly IReadOnlyList<KeyHint> DefaultHints =
    [
        new KeyHint("j/k", "scroll"),
        new KeyHint("y", "copy id"),
        new KeyHint("esc", "close")
    ];

    private readonly string _id;
    private readonly MemoryCollectionFilter _kind;
    private readonly IMemoryStore _memoryStore;
    private readonly TimeProvider _timeProvider;
    private readonly Viewport _bodyViewport = new(0, 0);

    private MemoryRecord? _curated;
    private MemoryJournalEntry? _journal;
    private string _lastAgeSignature = string.Empty;

    public MemoryEntryPopoverModel(
        string id, MemoryCollectionFilter kind, IMemoryStore memoryStore, TimeProvider timeProvider)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentNullException.ThrowIfNull(memoryStore);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _id = id;
        _kind = kind;
        _memoryStore = memoryStore;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public IReadOnlyList<KeyHint> Hints => DefaultHints;

    /// <summary>
    /// Loads (or reloads) the entry: <see cref="IMemoryStore.GetRequiredAsync"/> for a curated
    /// memory or <see cref="IMemoryStore.GetRequiredJournalEntryAsync"/> for a journal entry,
    /// blocking the caller.
    /// </summary>
    public void Load(CancellationToken cancellationToken = default) =>
        LoadAsync(cancellationToken).GetAwaiter().GetResult();

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (_kind == MemoryCollectionFilter.Curated)
        {
            _curated = await _memoryStore.GetRequiredAsync(_id, cancellationToken);
        }
        else
        {
            _journal = await _memoryStore.GetRequiredJournalEntryAsync(_id, cancellationToken);
        }

        _lastAgeSignature = ComputeAgeSignature(_timeProvider.GetUtcNow());
    }

    /// <summary>
    /// Recomputes the header's formatted ages as of now. Returns whether anything the render
    /// depends on changed since the last call.
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
        if (_curated is { } curated)
        {
            return $"{AgentAges.Format(curated.CreatedAt, now)}\u0001{AgentAges.Format(curated.UpdatedAt, now)}";
        }

        if (_journal is { } journal)
        {
            return AgentAges.Format(journal.CreatedAt, now);
        }

        return string.Empty;
    }

    /// <summary>
    /// Handles one raw key: j/k and the arrows scroll, g/G jump to the first or last line, y
    /// reports copying the entry id, and Escape closes the popover.
    /// </summary>
    public PopoverResult? HandleKey(ConsoleKeyInfo info)
    {
        switch (info.Key)
        {
            case ConsoleKey.J:
            case ConsoleKey.DownArrow:
                _bodyViewport.ScrollBy(1);
                return null;

            case ConsoleKey.K:
            case ConsoleKey.UpArrow:
                _bodyViewport.ScrollBy(-1);
                return null;

            case ConsoleKey.G when info.Modifiers == ConsoleModifiers.None:
                _bodyViewport.ScrollBy(int.MinValue / 2);
                return null;

            case ConsoleKey.G when info.Modifiers == ConsoleModifiers.Shift:
                _bodyViewport.ScrollBy(int.MaxValue / 2);
                return null;

            case ConsoleKey.Y when info.Modifiers == ConsoleModifiers.None:
                return new PopoverResult.Request(new MemoryEntryPopoverRequest.CopyRequested(_id));

            case ConsoleKey.Escape:
                return new PopoverResult.Closed();

            default:
                return null;
        }
    }

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
        var lines = MemoryEntryPopoverView.BuildLines(_curated, _journal, now, contentWidth);

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

        var header = MemoryEntryPopoverView.BuildHeader(_id);
        var panel = ColumnPane.RenderWithHeader(header, visible, focused: true);
        panel.Width = panelWidth;
        panel.Height = panelHeight;

        return new Align(panel, HorizontalAlignment.Center, VerticalAlignment.Middle)
            .Width(width)
            .Height(height);
    }
}

/// <summary>
/// Pure formatting for the memory entry popover: the panel title and the Kind, Type, Tags,
/// Created, Updated, and Promoted from header fields, followed by the wrapped body.
/// </summary>
internal static class MemoryEntryPopoverView
{
    /// <summary>
    /// A blank spacer row. A single space, not an empty string, so the panel's row renderer
    /// keeps it as a line instead of collapsing it away.
    /// </summary>
    private const string BlankLine = " ";

    private const string CuratedKindText = "curated";
    private const string JournalKindText = "journal";
    private const string NoTags = "-";

    /// <summary>
    /// The panel title: the entry id.
    /// </summary>
    public static string BuildHeader(string id) => Markup.Escape(id);

    /// <summary>
    /// Builds every line of the popover: a leading blank line, the header fields for the loaded
    /// curated memory or journal entry, a blank line, then the wrapped body.
    /// </summary>
    public static IReadOnlyList<string> BuildLines(
        MemoryRecord? curated, MemoryJournalEntry? journal, DateTimeOffset now, int width)
    {
        var lines = new List<string> { BlankLine };

        if (curated is { } record)
        {
            AppendCuratedFields(lines, record, now);
            lines.Add(BlankLine);
            AppendBody(lines, record.Body, width);
        }
        else if (journal is { } entry)
        {
            AppendJournalFields(lines, entry, now);
            lines.Add(BlankLine);
            AppendBody(lines, entry.Body, width);
        }

        return lines;
    }

    private static void AppendCuratedFields(List<string> lines, MemoryRecord record, DateTimeOffset now)
    {
        lines.Add(FormatField("Kind", CuratedKindText));
        lines.Add(FormatField("Type", record.Type));
        lines.Add(FormatField("Tags", record.Tags.Count == 0 ? NoTags : string.Join(", ", record.Tags)));
        lines.Add(FormatField("Created", $"{AgentAges.Format(record.CreatedAt, now)} by {record.CreatedBy}"));

        if (record.UpdatedAt != record.CreatedAt)
        {
            lines.Add(FormatField("Updated", AgentAges.Format(record.UpdatedAt, now)));
        }

        if (record.PromotedFrom is { } promotedFrom)
        {
            lines.Add(FormatField("Promoted from", promotedFrom));
        }
    }

    private static void AppendJournalFields(List<string> lines, MemoryJournalEntry entry, DateTimeOffset now)
    {
        lines.Add(FormatField("Kind", JournalKindText));
        lines.Add(FormatField("Created", $"{AgentAges.Format(entry.CreatedAt, now)} by {entry.CreatedBy}"));
    }

    private static void AppendBody(List<string> lines, string body, int width)
    {
        foreach (var bodyLine in TaskDetailSections.WrapText(body, width))
        {
            var escaped = Markup.Escape(bodyLine);
            lines.Add(escaped.Length == 0 ? BlankLine : escaped);
        }
    }

    private static string FormatField(string label, string value) =>
        $"[dim]{Markup.Escape(label)}:[/] {Markup.Escape(value)}";
}
