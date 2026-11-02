using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;
using Spectre.Console.Rendering;
using Form = ChilliCream.Nitro.CommandLine.Tui.Widgets.Form.Form;

namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// The Mail tab's search box: one text field pre-filled with
/// <see cref="MailState.SearchText"/>, applied as a case-insensitive filter over subject,
/// last sender, and last recipients.
/// </summary>
internal sealed class MailSearchForm
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

    public MailSearchForm(string initialText)
    {
        ArgumentNullException.ThrowIfNull(initialText);

        _textField = new TextField(TextFieldId, "Search subject, from, or to", initialValue: initialText);

        var buttons = new FormButtons(
        [
            new FormButtonSpec(ApplyButtonId, "Search", ButtonKind.Primary),
            new FormButtonSpec(CancelButtonId, "Cancel", ButtonKind.Secondary)
        ]);

        _form = new Form("Search mail", [_textField], buttons);
    }

    /// <summary>
    /// The field's current text.
    /// </summary>
    public string Text => _textField.GetValue() is FormValue.Text { Value: var value } ? value : "";

    public FormResult? HandleKey(ConsoleKeyInfo info) => _form.HandleKey(info);

    public IRenderable Render(int width, int height) => _form.Render(width, height);
}
