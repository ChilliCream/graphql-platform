using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Tui.Details;
using ChilliCream.Nitro.CommandLine.Tui.Theming;
using ChilliCream.Nitro.CommandLine.Tui.Widgets;
using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine.Tui.Memory;

/// <summary>
/// Renders the memory tab's detail pane: the selected curated memory's
/// frontmatter (id, scope, type, tags, timestamps, promoted-from) followed
/// by its markdown body word-wrapped as plain text, or the selected journal
/// entry's frontmatter (id, scope, created at/by) followed by its body, as a
/// scrollable body inside a bordered panel. Owns the body's scroll position;
/// <see cref="MemoryState"/> owns everything else. The body is rendered as
/// wrapped plain text, the same v1 decision <c>TaskDetailSections</c> makes
/// for task bodies, not as formatted markdown.
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
    /// Resets the body's scroll position to the top. Called whenever the
    /// pane's content changes: a different item is selected, the collection
    /// or scope filter changes, or the search box is applied.
    /// </summary>
    public void ResetScroll() => _bodyViewport.Update(0, 0);

    public IRenderable Render(MemoryState state, int width, int height, bool focused)
    {
        if (width <= 0 || height <= 0)
        {
            return new Markup(string.Empty);
        }

        var safeWidth = Math.Max(1, width);
        var interiorWidth = Math.Max(1, safeWidth - PanelChromeWidth);
        var interiorHeight = Math.Max(1, height - PanelChromeHeight);

        var lines = BuildLines(state, interiorWidth);

        IRenderable content = lines.Count == 0
            ? Align.Center(new Markup(Markup.Escape(NoSelectionMessage(state))), VerticalAlignment.Middle)
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
        MemoryCollectionFilter.Journal when state.SelectedJournalEntry is { } entry => BuildJournalLines(entry, width),
        _ => []
    };

    private static IReadOnlyList<string> BuildCuratedLines(MemoryRecord record, int width)
    {
        var lines = new List<string>
        {
            $"Scope: {record.Scope}",
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

    private static IReadOnlyList<string> BuildJournalLines(MemoryJournalEntry entry, int width)
    {
        var lines = new List<string>
        {
            $"Scope: {entry.Scope}",
            $"Created: {MemoryDates.Format(entry.CreatedAt)} by {entry.CreatedBy}",
            "Not yet promoted, or already promoted (press p to promote either way; a repeat is idempotent)."
        };

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
