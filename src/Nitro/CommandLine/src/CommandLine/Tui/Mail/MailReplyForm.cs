using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;
using Spectre.Console.Rendering;
using Form = ChilliCream.Nitro.CommandLine.Tui.Widgets.Form.Form;

namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// The reply form: body only. Recipient computation (reply-all, minus the
/// acting agent) is entirely owned by
/// <see cref="IMailStore.ReplyMessageAsync"/>, the same store member the
/// CLI's reply command calls, so the recipient set for a given message is
/// identical whether the reply was sent from the CLI or here. The host is
/// expected to feed it raw key input via <see cref="HandleKey"/> and call
/// <see cref="SubmitAsync"/> once it returns <see cref="FormResult.Submitted"/>
/// on the primary button.
/// </summary>
internal sealed class MailReplyForm
{
    public const string BodyFieldId = "body";

    public const string SendButtonId = "send";
    public const string CancelButtonId = "cancel";

    /// <summary>
    /// The footer hints for the reply form: its keys are consumed entirely
    /// while it is active, so no global hints follow.
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
    /// Whether the body field's current value differs from its blank
    /// default: gates whether Esc should ask for discard confirmation
    /// before cancelling.
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
    /// Sends the reply from the submitted <paramref name="values"/> through
    /// the mail store, carrying the message id this form was built with.
    /// </summary>
    public async Task<MailSendOutcome> SubmitAsync(
        IMailStore store,
        IReadOnlyDictionary<string, FormValue> values,
        string actor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentException.ThrowIfNullOrEmpty(actor);

        var body = Text(values, BodyFieldId);

        try
        {
            var message = await store.ReplyMessageAsync(_inReplyToId, actor, body, cancellationToken)
                .ConfigureAwait(false);

            return new MailSendOutcome.Succeeded(message.Id, $"Sent '{message.Id}'.");
        }
        catch (ExitException ex)
        {
            return new MailSendOutcome.Failed(ex.Message);
        }
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
