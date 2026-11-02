using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;
using Spectre.Console.Rendering;
using Form = ChilliCream.Nitro.CommandLine.Tui.Widgets.Form.Form;

namespace ChilliCream.Nitro.CommandLine.Tui.Memory;

/// <summary>
/// The memory tab's search box: one text field pre-filled with
/// <see cref="MemoryState.SearchText"/>, parsed by <see cref="MemoryQueryParser"/> once applied.
/// </summary>
internal sealed class MemorySearchForm
{
    public const string TextFieldId = "text";

    public const string ApplyButtonId = "apply";
    public const string CancelButtonId = "cancel";

    /// <summary>
    /// The footer hints displayed while the search form captures input.
    /// </summary>
    public static readonly IReadOnlyList<KeyHint> Hints =
    [
        new KeyHint("ctrl+s", "search"),
        new KeyHint("esc", "cancel")
    ];

    private readonly Form _form;
    private readonly TextField _textField;

    public MemorySearchForm(string initialText)
    {
        ArgumentNullException.ThrowIfNull(initialText);

        _textField = new TextField(TextFieldId, "Search (tag:x type:y free text)", initialValue: initialText);

        var buttons = new FormButtons(
        [
            new FormButtonSpec(ApplyButtonId, "Search", ButtonKind.Primary),
            new FormButtonSpec(CancelButtonId, "Cancel", ButtonKind.Secondary)
        ]);

        _form = new Form("Search", [_textField], buttons);
    }

    /// <summary>
    /// The field's current text.
    /// </summary>
    public string Text => _textField.GetValue() is FormValue.Text { Value: var value } ? value : "";

    public FormResult? HandleKey(ConsoleKeyInfo info) => _form.HandleKey(info);

    public IRenderable Render(int width, int height) => _form.Render(width, height);
}
