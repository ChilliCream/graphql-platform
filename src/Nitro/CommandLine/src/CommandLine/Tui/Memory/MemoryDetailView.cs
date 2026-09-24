using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Tui.Details;
using ChilliCream.Nitro.CommandLine.Tui.Theming;
using ChilliCream.Nitro.CommandLine.Tui.Widgets;
using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine.Tui.Memory;

/// <summary>
/// Renders the selected memory's metadata and wrapped body as plain text in a
/// scrollable detail panel.
/// </summary>
internal sealed class MemoryDetailView
{
    private const int PanelChromeWidth = 4;
    private const int PanelChromeHeight = 2;
    private const int MaxIndicatorSettlePasses = 3;

    private readonly Viewport _bodyViewport = new(0, 0);

    public void ScrollDown() => _bodyViewport.ScrollBy(1);

    public void ScrollUp() => _bodyViewport.ScrollBy(-1);

    public void ScrollToTop() => _bodyViewport.ScrollBy(int.MinValue / 2);

    public void ScrollToBottom() => _bodyViewport.ScrollBy(int.MaxValue / 2);

    /// <summary>
    /// Resets the body scroll position to the top.
    /// </summary>
    public void ResetScroll() => _bodyViewport.Update(0, 0);

    public IRenderable Render(MemoryState state, int width, int height, bool focused)
    {
        if (width <= 0 || height <= 0)
        {
            return new Markup(string.Empty);
        }

        var interiorWidth = Math.Max(1, Math.Max(1, width) - PanelChromeWidth);
        var lines = BuildLines(state, interiorWidth);

        return RenderPanel(lines, BuildHeader(state), NoSelectionMessage(state), width, height, focused);
    }

    /// <summary>
    /// Renders a curated memory directly, for a host outside the Memory tab that already
    /// has the record loaded (for example the agent detail popover).
    /// </summary>
    public IRenderable RenderCurated(MemoryRecord record, int width, int height, bool focused)
    {
        if (width <= 0 || height <= 0)
        {
            return new Markup(string.Empty);
        }

        var interiorWidth = Math.Max(1, Math.Max(1, width) - PanelChromeWidth);
        var header = $"[dim]{Markup.Escape(record.Id)}[/]";

        return RenderPanel(BuildCuratedLines(record, interiorWidth), header, "No item selected.", width, height, focused);
    }

    /// <summary>
    /// Renders a journal entry directly, for a host outside the Memory tab that already
    /// has the entry loaded (for example the agent detail popover).
    /// </summary>
    public IRenderable RenderJournal(MemoryJournalEntry entry, int width, int height, bool focused)
    {
        if (width <= 0 || height <= 0)
        {
            return new Markup(string.Empty);
        }

        var interiorWidth = Math.Max(1, Math.Max(1, width) - PanelChromeWidth);
        var header = $"[dim]{Markup.Escape(entry.Id)}[/]";
        var lines = BuildJournalLines(entry, interiorWidth, includePromoteHint: false);

        return RenderPanel(lines, header, "No item selected.", width, height, focused);
    }

    private IRenderable RenderPanel(
        IReadOnlyList<string> lines, string header, string emptyMessage, int width, int height, bool focused)
    {
        var safeWidth = Math.Max(1, width);
        var interiorHeight = Math.Max(1, height - PanelChromeHeight);

        IRenderable content = lines.Count == 0
            ? Align.Center(new Markup(Markup.Escape(emptyMessage)), VerticalAlignment.Middle)
            : new Rows(RenderVisibleLines(lines, interiorHeight).Select(Row));

        var borderToken = focused ? "board.column.border.focused" : "board.column.border";

        return new Panel(content)
        {
            Header = new PanelHeader(header),
            Border = BoxBorder.Rounded,
            BorderStyle = ThemeTokens.GetStyle(borderToken),
            Width = safeWidth,
            Height = Math.Max(1, height)
        };
    }

    private static string BuildHeader(MemoryState state) => state.Collection switch
    {
        MemoryCollectionFilter.Curated when state.SelectedCuratedRecord is { } record
            => $"[dim]{Markup.Escape(record.Id)}[/]",
        MemoryCollectionFilter.Journal when state.SelectedJournalEntry is { } entry
            => $"[dim]{Markup.Escape(entry.Id)}[/]",
        _ => "Detail"
    };

    private static string NoSelectionMessage(MemoryState state)
    {
        if (state.LoadError is { } error)
        {
            return error;
        }

        return state.ItemCount == 0
            ? state.Collection == MemoryCollectionFilter.Curated ? "No curated memories." : "No journal entries."
            : "No item selected.";
    }

    private static IReadOnlyList<string> BuildLines(MemoryState state, int width) => state.Collection switch
    {
        MemoryCollectionFilter.Curated when state.SelectedCuratedRecord is { } record => BuildCuratedLines(record, width),
        MemoryCollectionFilter.Journal when state.SelectedJournalEntry is { } entry
            => BuildJournalLines(entry, width, includePromoteHint: true),
        _ => []
    };

    private static IReadOnlyList<string> BuildCuratedLines(MemoryRecord record, int width)
    {
        var lines = new List<string>
        {
            $"Type: {record.Type}",
            $"Tags: {(record.Tags.Count == 0 ? "-" : string.Join(", ", record.Tags))}",
            $"Created: {MemoryDates.Format(record.CreatedAt)} by {record.CreatedBy}",
            $"Updated: {MemoryDates.Format(record.UpdatedAt)}"
        };

        if (record.PromotedFrom is { } promotedFrom)
        {
            lines.Add($"Promoted from: {promotedFrom}");
        }

        lines.Add(string.Empty);
        lines.AddRange(TaskDetailSections.WrapText(record.Body, width));

        return lines;
    }

    private static IReadOnlyList<string> BuildJournalLines(MemoryJournalEntry entry, int width, bool includePromoteHint)
    {
        var lines = new List<string> { $"Created: {MemoryDates.Format(entry.CreatedAt)} by {entry.CreatedBy}" };

        if (includePromoteHint)
        {
            lines.Add("Not yet promoted, or already promoted (press p to promote either way; a repeat is idempotent).");
        }

        lines.Add(string.Empty);
        lines.AddRange(TaskDetailSections.WrapText(entry.Body, width));

        return lines;
    }

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
