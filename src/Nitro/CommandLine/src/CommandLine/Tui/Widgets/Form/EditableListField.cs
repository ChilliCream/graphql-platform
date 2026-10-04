using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;

/// <summary>
/// An editable string list whose arrow keys traverse entries and release focus
/// at either end. Escape during an entry edit cancels that edit only.
/// </summary>
internal sealed class EditableListField : FormField
{
    private const string SelectedRowStyle = "aqua";
    private const string Bullet = "- ";

    private const int PanelBorderHeight = 2;
    private const int PanelBorderWidth = 4;

    private readonly List<string> _entries;

    private int _selectedIndex;
    private LineEditor? _editor;
    private string? _editingOriginalText;
    private bool _isEditingNewEntry;

    public EditableListField(
        string id,
        string label,
        bool required = false,
        IEnumerable<string>? initialValues = null,
        Func<FormValue, string?>? validator = null)
        : base(id, label, required, validator)
    {
        _entries = initialValues is null ? [] : [.. initialValues];
        _selectedIndex = _entries.Count == 0 ? -1 : 0;
    }

    private bool IsEditing => _editor is not null;

    public override FormValue GetValue() => new FormValue.List([.. _entries]);

    public override bool HandleKey(ConsoleKeyInfo info)
    {
        if (IsEditing)
        {
            return HandleEditingKey(info);
        }

        switch (info.Key)
        {
            case ConsoleKey.UpArrow:
                if (_selectedIndex <= 0)
                {
                    return false;
                }

                _selectedIndex--;
                return true;

            case ConsoleKey.DownArrow:
                if (_selectedIndex < 0 || _selectedIndex >= _entries.Count - 1)
                {
                    return false;
                }

                _selectedIndex++;
                return true;

            case ConsoleKey.Enter:
                if (_selectedIndex < 0)
                {
                    return false;
                }

                BeginEditing(_entries[_selectedIndex], isNewEntry: false);
                return true;

            case ConsoleKey.Delete:
                RemoveSelected();
                return true;

            default:
                if (info.KeyChar == 'a')
                {
                    AddEntry();
                    return true;
                }

                if (info.KeyChar == 'd')
                {
                    RemoveSelected();
                    return true;
                }

                return false;
        }
    }

    public override IRenderable Render(int width, bool focused)
        => RenderWithVisibleRows(width, focused, int.MaxValue);

    public override IRenderable Render(int width, bool focused, int maxHeight)
    {
        var errorHeight = ShowErrors && Validate() is not null ? 1 : 0;
        var visibleRows = Math.Max(1, maxHeight - PanelBorderHeight - errorHeight);

        return RenderWithVisibleRows(width, focused, visibleRows);
    }

    private IRenderable RenderWithVisibleRows(int width, bool focused, int visibleRows)
    {
        if (_entries.Count == 0)
        {
            return RenderPanel(new Markup(RenderPlaceholder("no labels - type to add")), width, focused);
        }

        var entryRenderables = new IRenderable[_entries.Count];
        var entryHeights = new int[_entries.Count];
        var contentWidth = Math.Max(1, width - PanelBorderWidth);

        for (var i = 0; i < _entries.Count; i++)
        {
            var isCurrentRow = focused && i == _selectedIndex;
            entryRenderables[i] = RenderEntry(i, isCurrentRow);
            entryHeights[i] = FormMeasurement.MeasureHeight(entryRenderables[i], contentWidth);
        }

        var (start, end) = SelectVisibleEntryRange(entryHeights, visibleRows);
        var rows = new List<IRenderable>(end - start + 1);

        for (var i = start; i <= end; i++)
        {
            var entry = entryRenderables[i];

            if (i == _selectedIndex && entryHeights[i] > visibleRows)
            {
                entry = new VisualRowWindow(entry, visibleRows, contentWidth);
            }

            rows.Add(entry);
        }

        return RenderPanel(new Rows(rows), width, focused);
    }

    private IRenderable RenderEntry(int index, bool isCurrentRow)
    {
        var text = IsEditing && index == _selectedIndex
            ? RenderEditingLine(_editor!)
            : Markup.Escape(_entries[index]);
        var line = Bullet + text;

        return new Markup(isCurrentRow ? $"[{SelectedRowStyle}]{line}[/]" : line);
    }

    private (int Start, int End) SelectVisibleEntryRange(IReadOnlyList<int> heights, int budget)
    {
        var start = _selectedIndex;
        var end = _selectedIndex;
        var used = Math.Min(heights[_selectedIndex], budget);

        while (start - 1 >= 0 && used + heights[start - 1] <= budget)
        {
            start--;
            used += heights[start];
        }

        while (end + 1 < heights.Count && used + heights[end + 1] <= budget)
        {
            end++;
            used += heights[end];
        }

        return (start, end);
    }

    private bool HandleEditingKey(ConsoleKeyInfo info)
    {
        switch (info.Key)
        {
            case ConsoleKey.Enter:
                CommitEditing();
                return true;

            case ConsoleKey.Escape:
                CancelEditing();
                return true;

            case ConsoleKey.UpArrow:
            case ConsoleKey.DownArrow:
                // Swallowed during editing.
                return true;

            default:
                if (_editor!.HandleKey(info))
                {
                    return true;
                }

                // Commit the entry and leave an unrecognized key for form navigation.
                CommitEditing();
                return false;
        }
    }

    private void BeginEditing(string text, bool isNewEntry)
    {
        _editor = new LineEditor(text);
        _editingOriginalText = text;
        _isEditingNewEntry = isNewEntry;
    }

    private void CommitEditing()
    {
        var text = _editor!.Text;

        if (text.Length == 0)
        {
            _entries.RemoveAt(_selectedIndex);
            _selectedIndex = Math.Min(_selectedIndex, _entries.Count - 1);
        }
        else
        {
            _entries[_selectedIndex] = text;
        }

        StopEditing();
    }

    private void CancelEditing()
    {
        if (_isEditingNewEntry)
        {
            _entries.RemoveAt(_selectedIndex);
            _selectedIndex = Math.Min(_selectedIndex, _entries.Count - 1);
        }
        else
        {
            _entries[_selectedIndex] = _editingOriginalText!;
        }

        StopEditing();
    }

    private void StopEditing()
    {
        _editor = null;
        _editingOriginalText = null;
        _isEditingNewEntry = false;
    }

    private void AddEntry()
    {
        var insertAt = _selectedIndex + 1;
        _entries.Insert(insertAt, string.Empty);
        _selectedIndex = insertAt;
        BeginEditing(string.Empty, isNewEntry: true);
    }

    private void RemoveSelected()
    {
        if (_selectedIndex < 0)
        {
            return;
        }

        _entries.RemoveAt(_selectedIndex);
        _selectedIndex = Math.Min(_selectedIndex, _entries.Count - 1);
    }

    private sealed class VisualRowWindow(IRenderable content, int visibleRows, int width) : IRenderable
    {
        public Measurement Measure(RenderOptions options, int maxWidth) => content.Measure(options, Math.Min(maxWidth, width));

        public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
        {
            var renderWidth = Math.Min(maxWidth, width);
            var lines = Segment.SplitLines(content.Render(options, renderWidth), renderWidth);
            var cursorLine = lines.FindIndex(line => line.Any(IsCursorSegment));

            if (cursorLine < 0)
            {
                cursorLine = 0;
            }

            var start = Math.Clamp(cursorLine - visibleRows + 1, 0, Math.Max(0, lines.Count - visibleRows));
            var end = Math.Min(lines.Count, start + visibleRows);
            var segments = new List<Segment>();

            for (var i = start; i < end; i++)
            {
                segments.AddRange(lines[i]);

                if (i < end - 1)
                {
                    segments.Add(Segment.LineBreak);
                }
            }

            return segments;
        }

        private static bool IsCursorSegment(Segment segment)
            => segment.Style.Foreground == Color.Black && segment.Style.Background == Color.White;
    }

    private static string RenderEditingLine(LineEditor editor)
    {
        const string cursorStyle = "black on white";

        var text = editor.Text;
        var cursor = editor.Cursor;

        var before = Markup.Escape(text[..cursor]);
        var atCursor = cursor < text.Length ? text[cursor].ToString() : " ";
        var after = cursor < text.Length ? Markup.Escape(text[(cursor + 1)..]) : string.Empty;

        return $"{before}[{cursorStyle}]{Markup.Escape(atCursor)}[/]{after}";
    }
}
