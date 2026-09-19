using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;
using Spectre.Console.Rendering;
using Form = ChilliCream.Nitro.CommandLine.Tui.Widgets.Form.Form;

namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// A body-only reply form. The mail store determines reply-all recipients when
/// the request is submitted.
/// </summary>
internal sealed class MailReplyForm
{
    public const string BodyFieldId = "body";

    public const string SendButtonId = "send";
    public const string CancelButtonId = "cancel";

    /// <summary>
    /// The footer hints displayed while the reply form captures input.
    /// </summary>
    public static readonly IReadOnlyList<KeyHint> Hints =
    [
        new KeyHint("ctrl+s", "send"),
        new KeyHint("esc", "cancel")
    ];

    private readonly string _inReplyToId;
    private readonly Form _form;
    private readonly TextAreaField _bodyField;

    public MailReplyForm(MailMessage original)
    {
        ArgumentNullException.ThrowIfNull(original);

        _inReplyToId = original.Id;
        _bodyField = new TextAreaField(BodyFieldId, "Body", required: true, validator: RequireBody);

        var buttons = new FormButtons(
        [
            new FormButtonSpec(SendButtonId, "Reply", ButtonKind.Primary),
            new FormButtonSpec(CancelButtonId, "Cancel", ButtonKind.Secondary)
        ]);

        _form = new Form($"Reply: {original.Subject}", [_bodyField], buttons);
    }

    /// <summary>
    /// Whether the body contains text, including whitespace.
    /// </summary>
    public bool IsDirty => Text(_bodyField).Length != 0;

    /// <summary>
    /// Handles one raw key. Returns <see langword="null"/> while the form is
    /// still active, or the terminal <see cref="FormResult"/> once the
    /// interaction ends.
    /// </summary>
    public FormResult? HandleKey(ConsoleKeyInfo info) => _form.HandleKey(info);

    /// <summary>
    /// Renders the form as a titled, rounded panel centered within the given
    /// area.
    /// </summary>
    public IRenderable Render(int width, int height) => _form.Render(width, height);

    /// <summary>
    /// Builds a reply request from the submitted body, acting agent, and original
    /// message id without writing to the store.
    /// </summary>
    public MailReplyRequest BuildRequest(IReadOnlyDictionary<string, FormValue> values, string actor)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentException.ThrowIfNullOrEmpty(actor);

        return new MailReplyRequest(_inReplyToId, actor, Text(values, BodyFieldId));
    }

    private static string? RequireBody(FormValue value)
        => value is FormValue.Text { Value: var text } && text.Trim().Length == 0
            ? "Body is required."
            : null;

    private static string Text(FormField field)
        => field.GetValue() is FormValue.Text { Value: var value } ? value : "";

    private static string Text(IReadOnlyDictionary<string, FormValue> values, string id)
        => values[id] is FormValue.Text { Value: var value } ? value : "";
}
