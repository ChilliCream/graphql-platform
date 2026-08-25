using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;

/// <summary>
/// One value a form field can produce: a single string, or an ordered list of
/// strings. Closed so consumers pattern-match on the shape instead of binding
/// through reflection.
/// </summary>
internal abstract record FormValue
{
    private FormValue()
    {
    }

    /// <summary>
    /// A single string value, produced by text, text area, and select fields.
    /// </summary>
    public sealed record Text(string Value) : FormValue;

    /// <summary>
    /// An ordered list of string values, produced by the editable list field.
    /// </summary>
    public sealed record List(IReadOnlyList<string> Values) : FormValue
    {
        /// <summary>
        /// Two lists are equal when they contain the same values in the same order.
        /// </summary>
        public bool Equals(List? other) => other is not null && Values.SequenceEqual(other.Values);

        /// <inheritdoc cref="Equals(List)" />
        public override int GetHashCode()
        {
            var hash = new HashCode();

            foreach (var value in Values)
            {
                hash.Add(value);
            }

            return hash.ToHashCode();
        }
    }
}

/// <summary>
/// One field in a <see cref="Form"/>: a labeled, bordered input that owns its own
/// editing state and can validate its current value.
/// </summary>
internal abstract class FormField
{
    private static readonly Style DefaultBorderStyle = new(Color.Grey);
    private static readonly Style FocusedBorderStyle = new(Color.Aqua);
    private static readonly Style ErrorBorderStyle = new(Color.Red);
    private static readonly Style ErrorTextStyle = new(Color.Red);

    private readonly Func<FormValue, string?>? _validator;

    protected FormField(string id, string label, bool required, Func<FormValue, string?>? validator)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentException.ThrowIfNullOrEmpty(label);

        Id = id;
        Label = label;
        Required = required;
        _validator = validator;
    }

    /// <summary>
    /// The identifier this field's value is keyed under in the <see cref="Form"/> result.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// The label shown in the field's border title.
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// Whether the field's title carries the required marker.
    /// </summary>
    public bool Required { get; }

    /// <summary>
    /// Whether <see cref="Render"/> should surface this field's validation
    /// error, if any. The hosting <see cref="Form"/> sets this once the field
    /// has been touched or a submit was attempted, so a required field stays
    /// quiet until the user has had a chance to fill it in.
    /// </summary>
    public bool ShowErrors { get; set; }

    /// <summary>
    /// The current value, in the closed <see cref="FormValue"/> shape for its field type.
    /// </summary>
    public abstract FormValue GetValue();

    /// <summary>
    /// Handles one key while this field has focus. Returns whether the key was
    /// consumed by the field; an unconsumed key is left for the form to interpret
    /// as focus traversal.
    /// </summary>
    public abstract bool HandleKey(ConsoleKeyInfo info);

    /// <summary>
    /// Renders the field as a bordered section, styled by its focus and validation
    /// state, at most <paramref name="width"/> display columns wide.
    /// </summary>
    public abstract IRenderable Render(int width, bool focused);

    /// <summary>
    /// Runs the field's validator against its current value, returning the error
    /// message to render under the field, or <see langword="null"/> when the value
    /// is valid or the field has no validator.
    /// </summary>
    public string? Validate() => _validator?.Invoke(GetValue());

    /// <summary>
    /// Renders <paramref name="text"/> as the dim placeholder line a field body
    /// shows in place of a blank line, so an empty field never collapses to a
    /// border pair with no interior line.
    /// </summary>
    protected static string RenderPlaceholder(string text) => $"[grey italic]{Markup.Escape(text)}[/]";

    /// <summary>
    /// Wraps <paramref name="content"/> in the bordered panel shared by every field
    /// renderer, with the field's title, focus-and-validation-driven border style,
    /// and validation error line appended underneath when present.
    /// </summary>
    protected IRenderable RenderPanel(IRenderable content, int width, bool focused)
    {
        var title = Required ? $"{Label} *" : Label;
        var error = ShowErrors ? Validate() : null;

        var borderStyle = error is not null
            ? ErrorBorderStyle
            : focused
                ? FocusedBorderStyle
                : DefaultBorderStyle;

        var panel = new Panel(content)
        {
            Header = new PanelHeader(Markup.Escape(title)),
            Border = BoxBorder.Rounded,
            BorderStyle = borderStyle
        };

        if (width > 0)
        {
            panel.Width = width;
        }

        if (error is null)
        {
            return panel;
        }

        var errorLine = new Markup($"[{ErrorTextStyle.ToMarkup()}]{Markup.Escape(error)}[/]");

        return new Rows(panel, errorLine);
    }
}

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
