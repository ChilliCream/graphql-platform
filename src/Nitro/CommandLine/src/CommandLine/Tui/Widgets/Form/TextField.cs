using ChilliCream.Nitro.CommandLine.Tui.Widgets;
using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;

/// <summary>
/// A single-line text field: cursor movement, insertion, deletion, and a
/// horizontal scroll window that keeps the cursor visible when the text is
/// wider than the field.
/// </summary>
internal sealed class TextField : FormField
{
    private const string CursorStyle = "black on white";
    private const int MinRenderWidth = 4;

    private readonly LineEditor _editor;
    private readonly Viewport _viewport;

    public TextField(
        string id,
        string label,
        bool required = false,
        string initialValue = "",
        Func<FormValue, string?>? validator = null)
        : base(id, label, required, validator)
    {
        _editor = new LineEditor(initialValue);

        // +1 so the cursor can sit one column past the last character.
        _viewport = new Viewport(_editor.Text.Length + 1, 1);
    }

    public override FormValue GetValue() => new FormValue.Text(_editor.Text);

    public override bool HandleKey(ConsoleKeyInfo info) => _editor.HandleKey(info);

    public override IRenderable Render(int width, bool focused)
    {
        var innerWidth = Math.Max(1, width - MinRenderWidth);

        _viewport.Update(_editor.Text.Length + 1, innerWidth);
        _viewport.EnsureVisible(_editor.Cursor);

        var (start, count) = _viewport.Slice();
        var visibleEnd = Math.Min(_editor.Text.Length, start + count);
        var visibleText = start >= _editor.Text.Length
            ? string.Empty
            : _editor.Text[start..visibleEnd];
        var visibleCursor = _editor.Cursor - start;

        var content = focused
            ? RenderWithCursor(visibleText, visibleCursor)
            : Markup.Escape(visibleText);

        return RenderPanel(new Markup(content), width, focused);
    }

    private static string RenderWithCursor(string visibleText, int cursor)
    {
        var before = Markup.Escape(visibleText[..cursor]);
        var atCursor = cursor < visibleText.Length ? visibleText[cursor].ToString() : " ";
        var after = cursor < visibleText.Length ? Markup.Escape(visibleText[(cursor + 1)..]) : string.Empty;

        return $"{before}[{CursorStyle}]{Markup.Escape(atCursor)}[/]{after}";
    }
}
