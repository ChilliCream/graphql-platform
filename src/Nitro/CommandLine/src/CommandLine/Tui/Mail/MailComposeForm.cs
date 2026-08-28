using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;
using Spectre.Console.Rendering;
using Form = ChilliCream.Nitro.CommandLine.Tui.Widgets.Form.Form;

namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// The compose form: recipients, subject, and body. Recipients are parsed
/// as a comma-separated list and validated by
/// <see cref="IMailStore.SendMessageAsync"/>, the same store member the
/// CLI's send command calls, so an unknown recipient surfaces as its
/// <see cref="ExitException"/> message rather than a client-side check. The
/// host is expected to feed it raw key input via <see cref="HandleKey"/>
/// and call <see cref="BuildCreation"/> once it returns
/// <see cref="FormResult.Submitted"/> on the primary button; the actual
/// store write and actor-wake dispatch run off the input thread through
/// <see cref="MailMode"/>'s own send effect, not synchronously from here.
/// </summary>
internal sealed class MailComposeForm
{
    public const string ToFieldId = "to";
    public const string SubjectFieldId = "subject";
    public const string BodyFieldId = "body";

    public const string SendButtonId = "send";
    public const string CancelButtonId = "cancel";

    /// <summary>
    /// The footer hints for the compose form: its keys are consumed
    /// entirely while it is active, so no global hints follow.
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
    /// Whether any field's current value differs from its blank default:
    /// gates whether Esc should ask for discard confirmation before
    /// cancelling.
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
    /// Snapshots the submitted <paramref name="values"/> into a
    /// <see cref="MailMessageCreation"/> ready for <see cref="IMailStore.SendMessageAsync"/>,
    /// with <see cref="MailMessageCreation.WakePolicy"/> set to
    /// <see cref="MailWakePolicy.Enqueue"/> so a board send behaves like the
    /// CLI's own send command: a live recipient is woken, not merely stored.
    /// Pure and synchronous; every field is already validated by the time a
    /// <see cref="FormResult.Submitted"/> carries these values, so this does
    /// no I/O and cannot fail.
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
