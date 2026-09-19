using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;
using Spectre.Console.Rendering;
using Form = ChilliCream.Nitro.CommandLine.Tui.Widgets.Form.Form;

namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// A form for comma-separated recipients, a required subject, and a message body.
/// Recipient names are validated by the mail store when submitted.
/// </summary>
internal sealed class MailComposeForm
{
    public const string ToFieldId = "to";
    public const string SubjectFieldId = "subject";
    public const string BodyFieldId = "body";

    public const string SendButtonId = "send";
    public const string CancelButtonId = "cancel";

    /// <summary>
    /// The footer hints displayed while the compose form captures input.
    /// </summary>
    public static readonly IReadOnlyList<KeyHint> Hints =
    [
        new KeyHint("tab", "next field"),
        new KeyHint("ctrl+s", "send"),
        new KeyHint("esc", "cancel")
    ];

    private readonly Form _form;
    private readonly TextField _toField;
    private readonly TextField _subjectField;
    private readonly TextAreaField _bodyField;

    public MailComposeForm()
    {
        _toField = new TextField(
            ToFieldId, "To (comma-separated)", required: true, validator: RequireRecipients);
        _subjectField = new TextField(
            SubjectFieldId, "Subject", required: true, validator: RequireSubject);
        _bodyField = new TextAreaField(BodyFieldId, "Body");

        var buttons = new FormButtons(
        [
            new FormButtonSpec(SendButtonId, "Send", ButtonKind.Primary),
            new FormButtonSpec(CancelButtonId, "Cancel", ButtonKind.Secondary)
        ]);

        _form = new Form("Compose", [_toField, _subjectField, _bodyField], buttons);
    }

    /// <summary>
    /// Whether any field contains text, including whitespace.
    /// </summary>
    public bool IsDirty
        => Text(_toField).Length != 0 || Text(_subjectField).Length != 0 || Text(_bodyField).Length != 0;

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
    /// Builds a message creation from submitted form values with
    /// <see cref="MailWakePolicy.Enqueue"/>. Does not write to the store.
    /// </summary>
    public static MailMessageCreation BuildCreation(IReadOnlyDictionary<string, FormValue> values, string actor)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentException.ThrowIfNullOrEmpty(actor);

        return new MailMessageCreation
        {
            Sender = actor,
            Subject = Text(values, SubjectFieldId),
            Body = Text(values, BodyFieldId),
            To = ParseRecipients(Text(values, ToFieldId)),
            WakePolicy = MailWakePolicy.Enqueue
        };
    }

    private static IReadOnlyList<string> ParseRecipients(string value)
        => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string? RequireRecipients(FormValue value)
        => value is FormValue.Text { Value: var text } && text.Trim().Length == 0
            ? "At least one recipient is required."
            : null;

    private static string? RequireSubject(FormValue value)
        => value is FormValue.Text { Value: var text } && text.Trim().Length == 0
            ? "Subject is required."
            : null;

    private static string Text(FormField field)
        => field.GetValue() is FormValue.Text { Value: var value } ? value : "";

    private static string Text(IReadOnlyDictionary<string, FormValue> values, string id)
        => values[id] is FormValue.Text { Value: var value } ? value : "";
}
