namespace ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;

/// <summary>
/// Single-line text edit state shared by <see cref="TextField"/> and
/// <see cref="EditableListField"/>: the current text and cursor position, and the
/// character-level operations a text input supports.
/// </summary>
internal sealed class LineEditor
{
    public LineEditor(string text = "")
    {
        Text = text;
        Cursor = text.Length;
    }

    /// <summary>
    /// The current text.
    /// </summary>
    public string Text { get; private set; }

    /// <summary>
    /// The cursor position, as an index into <see cref="Text"/> in the range
    /// <c>[0, Text.Length]</c>.
    /// </summary>
    public int Cursor { get; private set; }

    /// <summary>
    /// Applies <paramref name="info"/> to the text and cursor, returning whether the
    /// key was a recognized editing operation.
    /// </summary>
    public bool HandleKey(ConsoleKeyInfo info)
    {
        switch (info.Key)
        {
            case ConsoleKey.Backspace:
                if (Cursor > 0)
                {
                    Text = Text.Remove(Cursor - 1, 1);
                    Cursor--;
                }

                return true;

            case ConsoleKey.Delete:
                if (Cursor < Text.Length)
                {
                    Text = Text.Remove(Cursor, 1);
                }

                return true;

            case ConsoleKey.LeftArrow:
                if (Cursor > 0)
                {
                    Cursor--;
                }

                return true;

            case ConsoleKey.RightArrow:
                if (Cursor < Text.Length)
                {
                    Cursor++;
                }

                return true;

            case ConsoleKey.Home:
                Cursor = 0;
                return true;

            case ConsoleKey.End:
                Cursor = Text.Length;
                return true;

            default:
                if (IsInsertable(info.KeyChar))
                {
                    Text = Text.Insert(Cursor, info.KeyChar.ToString());
                    Cursor++;
                    return true;
                }

                return false;
        }
    }

    /// <summary>
    /// Replaces the text outright, moving the cursor to its end.
    /// </summary>
    public void SetText(string text)
    {
        Text = text;
        Cursor = text.Length;
    }

    /// <summary>
    /// Whether <paramref name="keyChar"/> is a printable character that a text
    /// input should insert, excluding control characters such as delete.
    /// </summary>
    public static bool IsInsertable(char keyChar) => keyChar >= ' ' && keyChar != '\u007f';
}
