using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;

/// <summary>
/// One field in a <see cref="Form"/>: a labeled, bordered input that owns its own
/// editing state and can validate its current value.
/// </summary>
internal abstract class FormField
{
    private static readonly Style s_defaultBorderStyle = new(Color.Grey);
    private static readonly Style s_focusedBorderStyle = new(Color.Aqua);
    private static readonly Style s_errorBorderStyle = new(Color.Red);
    private static readonly Style s_errorTextStyle = new(Color.Red);

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
            ? s_errorBorderStyle
            : focused
                ? s_focusedBorderStyle
                : s_defaultBorderStyle;

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

        var errorLine = new Markup($"[{s_errorTextStyle.ToMarkup()}]{Markup.Escape(error)}[/]");

        return new Rows(panel, errorLine);
    }
}
