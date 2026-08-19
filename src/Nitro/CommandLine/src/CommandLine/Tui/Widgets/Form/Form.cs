using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;

file static class FormMeasurement
{
    /// <summary>
    /// An off-screen console used only to obtain a <see cref="RenderOptions"/> for
    /// measuring a renderable's line count; nothing is ever written through it.
    /// </summary>
    private static readonly IAnsiConsole Console =
        AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(TextWriter.Null) });

    private static readonly RenderOptions Options = RenderOptions.Create(Console, Console.Profile.Capabilities);

    /// <summary>
    /// Renders <paramref name="renderable"/> at <paramref name="width"/> and counts
    /// the visual lines it occupies.
    /// </summary>
    public static int MeasureHeight(IRenderable renderable, int width)
        => Segment.SplitLines(renderable.Render(Options, Math.Max(1, width))).Count;
}

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

    /// <summary>
    /// The narrowest a form's content ever renders once the frame clears
    /// <see cref="MinViableWidth"/>.
    /// </summary>
    private const int MinFormWidth = 20;

    /// <summary>
    /// The frame width below which <see cref="Render"/> gives up on laying out
    /// the form and shows the too-small notice instead.
    /// </summary>
    private const int MinViableWidth = 24;

    /// <summary>
    /// The frame height below which <see cref="Render"/> gives up on laying out
    /// the form and shows the too-small notice instead.
    /// </summary>
    private const int MinViableHeight = 10;

    /// <summary>
    /// Rows of breathing room left between the modal and the edges of the frame.
    /// </summary>
    private const int FrameMargin = 2;

    /// <summary>
    /// Rows the panel's own top and bottom border occupy.
    /// </summary>
    private const int PanelBorderHeight = 2;

    /// <summary>
    /// Rows the blank line between the fields and the button row occupies.
    /// </summary>
    private const int SeparatorHeight = 1;

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
        if (IsSaveChord(info))
        {
            return TrySave();
        }

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
    /// Renders the form as a titled, rounded panel centered within the given
    /// area: clamped to at most <see cref="MaxFormWidth"/> columns and to the
    /// frame's own height, scrolling the field list, when it does not fit, to
    /// keep the focused field fully visible while the button row stays pinned
    /// at the bottom. Below <see cref="MinViableWidth"/> or
    /// <see cref="MinViableHeight"/>, renders a too-small notice instead.
    /// </summary>
    public IRenderable Render(int width, int height)
    {
        if (width < MinViableWidth || height < MinViableHeight)
        {
            return RenderTooSmallNotice(width, height);
        }

        var formWidth = Math.Clamp(width - 4, MinFormWidth, MaxFormWidth - 4);

        var fieldRenderables = new IRenderable[_fields.Count];
        var fieldHeights = new int[_fields.Count];

        for (var i = 0; i < _fields.Count; i++)
        {
            _fields[i].ShowErrors = _submitAttempted || _touched[i];
            var renderable = _fields[i].Render(formWidth, focused: IsFieldFocused && i == _focusIndex);
            fieldRenderables[i] = renderable;
            fieldHeights[i] = FormMeasurement.MeasureHeight(renderable, formWidth);
        }

        var buttonsRenderable = _buttons.Render(focused: !IsFieldFocused);
        var buttonsHeight = FormMeasurement.MeasureHeight(buttonsRenderable, formWidth);

        var chrome = PanelBorderHeight + SeparatorHeight + buttonsHeight;
        var totalFieldsHeight = fieldHeights.Sum();
        // Guarantees room for the chrome plus at least one field row, even if
        // the frame barely clears MinViableHeight.
        var maxPanelHeight = Math.Max(chrome + 1, height - FrameMargin);
        var availableFieldsHeight = Math.Max(0, maxPanelHeight - chrome);
        var needsScrolling = totalFieldsHeight > availableFieldsHeight;

        // Reserve room for the scroll indicators only when scrolling is
        // actually happening, so a form that fits entirely never loses a row
        // to indicators it doesn't show.
        var windowBudget = needsScrolling
            ? Math.Max(0, availableFieldsHeight - 2)
            : availableFieldsHeight;

        var anchorIndex = IsFieldFocused ? _focusIndex : _fields.Count - 1;
        var (startIndex, endIndex) = SelectVisibleFieldRange(fieldHeights, anchorIndex, windowBudget);
        var hiddenAbove = startIndex > 0;
        var hiddenBelow = endIndex < _fields.Count - 1;

        var sections = new List<IRenderable>(endIndex - startIndex + 4);

        if (hiddenAbove)
        {
            sections.Add(new Markup("[grey italic]▲ more fields above[/]"));
        }

        for (var i = startIndex; i <= endIndex; i++)
        {
            sections.Add(fieldRenderables[i]);
        }

        if (hiddenBelow)
        {
            sections.Add(new Markup("[grey italic]▼ more fields below[/]"));
        }

        // A blank line separates the fields from the button row so the buttons
        // don't read as crowded against the last field.
        sections.Add(new Markup(" "));
        sections.Add(buttonsRenderable);

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

    /// <summary>
    /// Picks the widest contiguous run of whole fields, anchored at
    /// <paramref name="anchorIndex"/>, whose combined <paramref name="heights"/>
    /// fit within <paramref name="budget"/>: the anchor field is always
    /// included even when it alone exceeds the budget.
    /// </summary>
    private static (int Start, int End) SelectVisibleFieldRange(
        IReadOnlyList<int> heights, int anchorIndex, int budget)
    {
        var start = anchorIndex;
        var end = anchorIndex;
        var used = heights[anchorIndex];

        while (end + 1 < heights.Count && used + heights[end + 1] <= budget)
        {
            end++;
            used += heights[end];
        }

        while (start - 1 >= 0 && used + heights[start - 1] <= budget)
        {
            start--;
            used += heights[start];
        }

        return (start, end);
    }

    /// <summary>
    /// Renders a centered notice in place of the form when the frame is too
    /// small to lay out any field usefully.
    /// </summary>
    private static IRenderable RenderTooSmallNotice(int width, int height)
    {
        var notice = new Markup("[red]Terminal too small.[/] Resize to at least 80x24.");

        return new Align(notice, HorizontalAlignment.Center, VerticalAlignment.Middle)
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

        return _buttons.SelectedKind != ButtonKind.Primary
            ? new FormResult.ButtonActivated(id)
            : TryValidateAndSubmit();
    }

    /// <summary>
    /// Whether <paramref name="info"/> is the save chord that submits the form
    /// from any focus position, as if the primary button were activated:
    /// Ctrl+Enter, or Ctrl+S as a fallback for terminals whose legacy input
    /// mode reports Ctrl+Enter identically to a plain Enter.
    /// </summary>
    private static bool IsSaveChord(ConsoleKeyInfo info)
        => info.Modifiers.HasFlag(ConsoleModifiers.Control)
        && info.Key is ConsoleKey.Enter or ConsoleKey.S;

    /// <summary>
    /// Submits the form as if the primary button were activated, regardless of
    /// which field or button currently has focus. On validation failure,
    /// moves focus to the first invalid field instead of leaving focus
    /// wherever the chord was pressed, so the surfaced error is immediately
    /// visible.
    /// </summary>
    private FormResult? TrySave()
    {
        var result = TryValidateAndSubmit();

        if (result is null)
        {
            _focusIndex = FirstInvalidFieldIndex();
        }

        return result;
    }

    /// <summary>
    /// Validates every field, marking the form as having attempted a submit so
    /// every field's error becomes visible; returns the submitted values when
    /// all fields are valid, or <see langword="null"/> to keep the form open
    /// otherwise.
    /// </summary>
    private FormResult? TryValidateAndSubmit()
    {
        _submitAttempted = true;

        if (_fields.Any(field => field.Validate() is not null))
        {
            return null;
        }

        return new FormResult.Submitted(_fields.ToDictionary(field => field.Id, field => field.GetValue()));
    }

    private int FirstInvalidFieldIndex()
    {
        for (var i = 0; i < _fields.Count; i++)
        {
            if (_fields[i].Validate() is not null)
            {
                return i;
            }
        }

        // TrySave only calls this after TryValidateAndSubmit found an invalid
        // field, so one is always present here.
        throw new InvalidOperationException("Expected an invalid field.");
    }
}
