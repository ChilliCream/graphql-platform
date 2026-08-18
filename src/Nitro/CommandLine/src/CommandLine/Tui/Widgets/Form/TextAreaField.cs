using ChilliCream.Nitro.CommandLine.Tui.Widgets;
using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;

/// <summary>
/// A multi-line plain-text field: insertion, newlines, backspace, cursor
/// movement across lines, and a vertical scroll window over the visible rows.
/// Up and down move the cursor within the text until it reaches the first or
/// last line, at which point the key is left unconsumed so focus can move on.
/// </summary>
internal sealed class TextAreaField : FormField
{
    private const int MinRenderWidth = 4;

    private const string CursorStyle = "black on white";

    private readonly List<string> _lines;
    private readonly Viewport _rowViewport;
    private readonly Viewport _columnViewport = new(0, 1);
    private readonly int _visibleLines;

    private int _lineIndex;
    private int _columnIndex;

    public TextAreaField(
        string id,
        string label,
        bool required = false,
        string initialValue = "",
        int visibleLines = 4,
        Func<FormValue, string?>? validator = null)
        : base(id, label, required, validator)
    {
        _lines = initialValue.Length == 0 ? [""] : [.. initialValue.Split('\n')];
        _visibleLines = Math.Max(1, visibleLines);
        _rowViewport = new Viewport(_lines.Count, _visibleLines);
        _columnIndex = _lines[^1].Length;
        _lineIndex = _lines.Count - 1;
    }

    public override FormValue GetValue() => new FormValue.Text(string.Join('\n', _lines));

    public override bool HandleKey(ConsoleKeyInfo info)
    {
        switch (info.Key)
        {
            case ConsoleKey.Enter:
                SplitLine();
                return true;

            case ConsoleKey.Backspace:
                DeleteBackward();
                return true;

            case ConsoleKey.Delete:
                DeleteForward();
                return true;

            case ConsoleKey.LeftArrow:
                MoveLeft();
                return true;

            case ConsoleKey.RightArrow:
                MoveRight();
                return true;

            case ConsoleKey.Home:
                _columnIndex = 0;
                return true;

            case ConsoleKey.End:
                _columnIndex = CurrentLine.Length;
                return true;

            case ConsoleKey.UpArrow:
                if (_lineIndex == 0)
                {
                    return false;
                }

                _lineIndex--;
                _columnIndex = Math.Min(_columnIndex, CurrentLine.Length);
                return true;

            case ConsoleKey.DownArrow:
                if (_lineIndex == _lines.Count - 1)
                {
                    return false;
                }

                _lineIndex++;
                _columnIndex = Math.Min(_columnIndex, CurrentLine.Length);
                return true;

            default:
                if (LineEditor.IsInsertable(info.KeyChar))
                {
                    _lines[_lineIndex] = CurrentLine.Insert(_columnIndex, info.KeyChar.ToString());
                    _columnIndex++;
                    return true;
                }

                return false;
        }
    }

    public override IRenderable Render(int width, bool focused)
    {
        var innerWidth = Math.Max(1, width - MinRenderWidth);

        _rowViewport.Update(_lines.Count, _visibleLines);
        _rowViewport.EnsureVisible(_lineIndex);

        var (start, count) = _rowViewport.Slice();
        var rows = new List<IRenderable>();

        for (var i = start; i < start + count; i++)
        {
            var isCursorRow = focused && i == _lineIndex;
            var markup = isCursorRow
                ? RenderCursorLine(_lines[i], _columnIndex, innerWidth)
                : Markup.Escape(Truncate(_lines[i], innerWidth));

            rows.Add(new Markup(markup));
        }

        var content = rows.Count == 0 ? (IRenderable)new Markup(string.Empty) : new Rows(rows);

        return RenderPanel(content, width, focused);
    }

    private string CurrentLine => _lines[_lineIndex];

    private void SplitLine()
    {
        var line = CurrentLine;
        var before = line[.._columnIndex];
        var after = line[_columnIndex..];

        _lines[_lineIndex] = before;
        _lines.Insert(_lineIndex + 1, after);
        _lineIndex++;
        _columnIndex = 0;
    }

    private void DeleteBackward()
    {
        if (_columnIndex > 0)
        {
            _lines[_lineIndex] = CurrentLine.Remove(_columnIndex - 1, 1);
            _columnIndex--;
            return;
        }

        if (_lineIndex == 0)
        {
            return;
        }

        var mergedColumn = _lines[_lineIndex - 1].Length;
        _lines[_lineIndex - 1] += CurrentLine;
        _lines.RemoveAt(_lineIndex);
        _lineIndex--;
        _columnIndex = mergedColumn;
    }

    private void DeleteForward()
    {
        if (_columnIndex < CurrentLine.Length)
        {
            _lines[_lineIndex] = CurrentLine.Remove(_columnIndex, 1);
            return;
        }

        if (_lineIndex == _lines.Count - 1)
        {
            return;
        }

        _lines[_lineIndex] += _lines[_lineIndex + 1];
        _lines.RemoveAt(_lineIndex + 1);
    }

    private void MoveLeft()
    {
        if (_columnIndex > 0)
        {
            _columnIndex--;
            return;
        }

        if (_lineIndex == 0)
        {
            return;
        }

        _lineIndex--;
        _columnIndex = CurrentLine.Length;
    }

    private void MoveRight()
    {
        if (_columnIndex < CurrentLine.Length)
        {
            _columnIndex++;
            return;
        }

        if (_lineIndex == _lines.Count - 1)
        {
            return;
        }

        _lineIndex++;
        _columnIndex = 0;
    }

    private string RenderCursorLine(string line, int cursorColumn, int innerWidth)
    {
        // +1 so the cursor can sit one column past the last character.
        _columnViewport.Update(line.Length + 1, innerWidth);
        _columnViewport.EnsureVisible(cursorColumn);

        var (start, count) = _columnViewport.Slice();
        var visibleEnd = Math.Min(line.Length, start + count);
        var visibleText = start >= line.Length ? string.Empty : line[start..visibleEnd];
        var visibleCursor = cursorColumn - start;

        var before = Markup.Escape(visibleText[..visibleCursor]);
        var atCursor = visibleCursor < visibleText.Length ? visibleText[visibleCursor].ToString() : " ";
        var after = visibleCursor < visibleText.Length ? Markup.Escape(visibleText[(visibleCursor + 1)..]) : string.Empty;

        return $"{before}[{CursorStyle}]{Markup.Escape(atCursor)}[/]{after}";
    }

    private static string Truncate(string value, int width)
    {
        if (width <= 0 || value.Length <= width)
        {
            return value;
        }

        return value[..Math.Max(0, width - 1)] + "…";
    }
}
