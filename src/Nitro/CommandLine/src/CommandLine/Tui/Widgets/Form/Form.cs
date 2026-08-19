using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;

/// <summary>
/// The outcome of a completed <see cref="Form"/> interaction.
/// </summary>
internal abstract record FormResult
{
    private FormResult()
    {
    }

    /// <summary>
    /// The primary button was activated while every field passed validation.
    /// </summary>
    public sealed record Submitted(IReadOnlyDictionary<string, FormValue> Values) : FormResult;

    /// <summary>
    /// The form was cancelled: Escape was pressed while no field consumed it.
    /// </summary>
    public sealed record Cancelled : FormResult;

    /// <summary>
    /// A non-primary button was activated. The <see cref="Form"/> does not
    /// interpret the id; the host decides what it means.
    /// </summary>
    public sealed record ButtonActivated(string ButtonId) : FormResult;
}

/// <summary>
/// A scoped modal form: labeled bordered fields with focus traversal, per-field
/// validation, and a confirm/cancel button row. Rendered as a single-column,
/// centered overlay; the host is expected to feed it raw key input and stop
/// showing it once <see cref="HandleKey"/> returns a non-null result.
/// </summary>
internal sealed class Form
{
    /// <summary>
    /// The widest a form's content is ever rendered, regardless of how much
    /// wider the frame around it is.
    /// </summary>
    private const int MaxFormWidth = 80;

    private readonly string _title;
    private readonly IReadOnlyList<FormField> _fields;
    private readonly FormButtons _buttons;
    private readonly int _stopCount;
    private readonly bool[] _touched;

    private int _focusIndex;
    private bool _submitAttempted;

    public Form(string title, IReadOnlyList<FormField> fields, FormButtons buttons)
    {
        ArgumentException.ThrowIfNullOrEmpty(title);
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(buttons);

        if (fields.Count == 0)
        {
            throw new ArgumentException("A form must have at least one field.", nameof(fields));
        }

        _title = title;
        _fields = fields;
        _buttons = buttons;
        _touched = new bool[_fields.Count];

        // Every field is one focus stop, plus one more for the button row.
        _stopCount = _fields.Count + 1;
    }

    /// <summary>
    /// The field currently holding focus, or <see langword="null"/> when focus is
    /// on the button row.
    /// </summary>
    public FormField? FocusedField => IsFieldFocused ? _fields[_focusIndex] : null;

    private bool IsFieldFocused => _focusIndex < _fields.Count;

    /// <summary>
    /// Handles one raw key. Returns <see langword="null"/> while the form is still
    /// active, or the terminal <see cref="FormResult"/> once the interaction ends.
    /// </summary>
    public FormResult? HandleKey(ConsoleKeyInfo info)
    {
        if (IsFieldFocused)
        {
            var field = _fields[_focusIndex];

            if (field.HandleKey(info))
            {
                _touched[_focusIndex] = true;
                return null;
            }

            return info.Key == ConsoleKey.Escape
                ? new FormResult.Cancelled()
                : HandleTraversal(info);
        }

        if (_buttons.HandleKey(info))
        {
            return null;
        }

        return info.Key switch
        {
            ConsoleKey.Escape => new FormResult.Cancelled(),
            ConsoleKey.Enter => ActivateSelectedButton(),
            _ => HandleTraversal(info)
        };
    }

    /// <summary>
    /// Renders the form as a titled, rounded panel centered within the given area.
    /// </summary>
    public IRenderable Render(int width, int height)
    {
        var formWidth = Math.Clamp(width - 4, 20, MaxFormWidth - 4);

        var sections = new List<IRenderable>(_stopCount + 1);

        for (var i = 0; i < _fields.Count; i++)
        {
            _fields[i].ShowErrors = _submitAttempted || _touched[i];
            sections.Add(_fields[i].Render(formWidth, focused: IsFieldFocused && i == _focusIndex));
        }

        // A blank line separates the fields from the button row so the buttons
        // don't read as crowded against the last field.
        sections.Add(new Markup(" "));
        sections.Add(_buttons.Render(focused: !IsFieldFocused));

        var panel = new Panel(new Rows(sections))
        {
            Header = new PanelHeader(Markup.Escape(_title)),
            Border = BoxBorder.Rounded,
            Width = formWidth + 4
        };

        return new Align(panel, HorizontalAlignment.Center, VerticalAlignment.Middle)
            .Width(Math.Max(0, width))
            .Height(Math.Max(0, height));
    }

    private FormResult? HandleTraversal(ConsoleKeyInfo info)
    {
        switch (info.Key)
        {
            case ConsoleKey.Tab when info.Modifiers.HasFlag(ConsoleModifiers.Shift):
            case ConsoleKey.UpArrow:
                FocusPrevious();
                return null;

            case ConsoleKey.Tab:
            case ConsoleKey.DownArrow:
                FocusNext();
                return null;

            default:
                return null;
        }
    }

    private void FocusNext() => _focusIndex = (_focusIndex + 1) % _stopCount;

    private void FocusPrevious() => _focusIndex = (_focusIndex - 1 + _stopCount) % _stopCount;

    private FormResult? ActivateSelectedButton()
    {
        var id = _buttons.SelectedId;

        if (_buttons.SelectedKind != ButtonKind.Primary)
        {
            return new FormResult.ButtonActivated(id);
        }

        _submitAttempted = true;

        if (_fields.Any(field => field.Validate() is not null))
        {
            return null;
        }

        return new FormResult.Submitted(_fields.ToDictionary(field => field.Id, field => field.GetValue()));
    }
}
