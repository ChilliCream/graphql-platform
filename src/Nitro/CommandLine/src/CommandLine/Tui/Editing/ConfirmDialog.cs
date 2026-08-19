using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;
using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine.Tui.Editing;

/// <summary>
/// The outcome of a completed <see cref="ConfirmDialog"/> interaction.
/// </summary>
internal abstract record ConfirmDialogResult
{
    private ConfirmDialogResult()
    {
    }

    /// <summary>
    /// The confirm button was activated, or Enter was pressed while the
    /// reason field had focus. Carries the reason text entered, or an empty
    /// string when left blank.
    /// </summary>
    public sealed record Confirmed(string Reason) : ConfirmDialogResult;

    /// <summary>
    /// The dialog was cancelled: Escape was pressed, or the cancel button was
    /// activated.
    /// </summary>
    public sealed record Cancelled : ConfirmDialogResult;
}

/// <summary>
/// A confirmation modal with a message, an optional single-line reason
/// <see cref="TextField"/>, and a confirm/cancel button row. Escape cancels
/// from anywhere; Enter confirms from the reason field regardless of which
/// button is selected, or activates whichever button has focus. The host is
/// expected to feed it raw key input and stop showing it once
/// <see cref="HandleKey"/> returns a non-null result.
/// </summary>
internal sealed class ConfirmDialog
{
    private const string ConfirmId = "confirm";
    private const string CancelId = "cancel";

    private readonly TextField _reasonField;
    private readonly FormButtons _buttons;

    private bool _fieldFocused = true;

    public ConfirmDialog(
        string message,
        string confirmLabel,
        ButtonKind confirmKind = ButtonKind.Primary,
        string cancelLabel = "Cancel")
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
        ArgumentException.ThrowIfNullOrEmpty(confirmLabel);
        ArgumentException.ThrowIfNullOrEmpty(cancelLabel);

        Message = message;
        _reasonField = new TextField("reason", "Reason (optional)");
        _buttons = new FormButtons(
        [
            new FormButtonSpec(ConfirmId, confirmLabel, confirmKind),
            new FormButtonSpec(CancelId, cancelLabel, ButtonKind.Secondary)
        ]);
    }

    /// <summary>
    /// The confirmation prompt shown above the reason field.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// The footer hints for this dialog: its keys are consumed entirely
    /// while it is active, so no global hints follow.
    /// </summary>
    public static readonly IReadOnlyList<KeyHint> Hints =
    [
        new KeyHint("enter", "confirm"),
        new KeyHint("esc", "cancel")
    ];

    /// <summary>
    /// Handles one raw key. Returns <see langword="null"/> while the dialog is
    /// still active, or the terminal <see cref="ConfirmDialogResult"/> once
    /// the interaction ends.
    /// </summary>
    public ConfirmDialogResult? HandleKey(ConsoleKeyInfo info)
    {
        if (_fieldFocused)
        {
            if (_reasonField.HandleKey(info))
            {
                return null;
            }

            return info.Key switch
            {
                ConsoleKey.Escape => new ConfirmDialogResult.Cancelled(),
                ConsoleKey.Enter => new ConfirmDialogResult.Confirmed(GetReason()),
                _ => HandleTraversal(info)
            };
        }

        if (_buttons.HandleKey(info))
        {
            return null;
        }

        return info.Key switch
        {
            ConsoleKey.Escape => new ConfirmDialogResult.Cancelled(),
            ConsoleKey.Enter => ActivateSelectedButton(),
            _ => HandleTraversal(info)
        };
    }

    /// <summary>
    /// Renders the dialog as a rounded panel, centered within the given area.
    /// </summary>
    public IRenderable Render(int width, int height)
    {
        var panelWidth = Math.Clamp(width - 4, 20, 96);

        var sections = new List<IRenderable>
        {
            new Markup(Markup.Escape(Message)),
            _reasonField.Render(panelWidth, focused: _fieldFocused),
            _buttons.Render(focused: !_fieldFocused)
        };

        var panel = new Panel(new Rows(sections))
        {
            Border = BoxBorder.Rounded,
            Width = panelWidth + 4
        };

        return new Align(panel, HorizontalAlignment.Center, VerticalAlignment.Middle)
            .Width(Math.Max(0, width))
            .Height(Math.Max(0, height));
    }

    private ConfirmDialogResult? HandleTraversal(ConsoleKeyInfo info)
    {
        switch (info.Key)
        {
            case ConsoleKey.Tab:
            case ConsoleKey.UpArrow:
            case ConsoleKey.DownArrow:
                _fieldFocused = !_fieldFocused;
                return null;

            default:
                return null;
        }
    }

    private ConfirmDialogResult? ActivateSelectedButton()
        => _buttons.SelectedId == CancelId
            ? new ConfirmDialogResult.Cancelled()
            : new ConfirmDialogResult.Confirmed(GetReason());

    private string GetReason()
        => _reasonField.GetValue() is FormValue.Text { Value: var value } ? value : "";
}
