using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;
using Spectre.Console.Rendering;
using Form = ChilliCream.Nitro.CommandLine.Tui.Widgets.Form.Form;

namespace ChilliCream.Nitro.CommandLine.Tui.Memory;

/// <summary>
/// The promote form: type (required) and tags. Mechanical copy only, no
/// summarization: submitting calls <see cref="IMemoryStore.PromoteAsync"/>,
/// the same store member the CLI's <c>promote</c> command calls, in the
/// journal entry's own scope. A repeat promotion of the same journal entry
/// is idempotent, surfaced as <see cref="MemoryPromoteOutcome.Succeeded.AlreadyPromoted"/>
/// rather than an error. The host is expected to feed it raw key input via
/// <see cref="HandleKey"/> and call <see cref="SubmitAsync"/> once it
/// returns <see cref="FormResult.Submitted"/> on the primary button.
/// </summary>
internal sealed class MemoryPromoteForm
{
    public const string TypeFieldId = "type";
    public const string TagsFieldId = "tags";

    public const string PromoteButtonId = "promote";
    public const string CancelButtonId = "cancel";

    /// <summary>
    /// The footer hints for the promote form: its keys are consumed entirely
    /// while it is active, so no global hints follow.
    /// </summary>
    public static readonly IReadOnlyList<KeyHint> Hints =
    [
        new KeyHint("ctrl+s", "promote"),
        new KeyHint("esc", "cancel")
    ];

    private readonly string _journalId;
    private readonly string _scope;
    private readonly Form _form;
    private readonly TextField _typeField;
    private readonly TextField _tagsField;

    public MemoryPromoteForm(MemoryJournalEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        _journalId = entry.Id;
        _scope = entry.Scope;
        _typeField = new TextField(TypeFieldId, "Type", required: true, validator: RequireType);
        _tagsField = new TextField(TagsFieldId, "Tags (comma separated)");

        var buttons = new FormButtons(
        [
            new FormButtonSpec(PromoteButtonId, "Promote", ButtonKind.Primary),
            new FormButtonSpec(CancelButtonId, "Cancel", ButtonKind.Secondary)
        ]);

        _form = new Form($"Promote: {entry.Id}", [_typeField, _tagsField], buttons);
    }

    /// <summary>
    /// Whether the type or tags field's current value differs from its
    /// blank default: gates whether Esc should ask for discard confirmation
    /// before cancelling.
    /// </summary>
    public bool IsDirty => Text(_typeField).Length != 0 || Text(_tagsField).Length != 0;

    public FormResult? HandleKey(ConsoleKeyInfo info) => _form.HandleKey(info);

    public IRenderable Render(int width, int height) => _form.Render(width, height);

    /// <summary>
    /// Promotes the journal entry this form was built with from the
    /// submitted <paramref name="values"/> through the memory store.
    /// </summary>
    public async Task<MemoryPromoteOutcome> SubmitAsync(
        IMemoryStore store, IReadOnlyDictionary<string, FormValue> values, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(values);

        var type = Text(values, TypeFieldId);
        var tags = ParseTags(Text(values, TagsFieldId));

        try
        {
            var outcome = await store.PromoteAsync(_journalId, _scope, type, tags, cancellationToken)
                .ConfigureAwait(false);

            var toastText = outcome.AlreadyPromoted
                ? $"Journal entry '{_journalId}' was already promoted as '{outcome.Record.Id}'."
                : $"Promoted memory '{outcome.Record.Id}'.";

            return new MemoryPromoteOutcome.Succeeded(outcome.Record.Id, outcome.AlreadyPromoted, toastText);
        }
        catch (ExitException ex)
        {
            return new MemoryPromoteOutcome.Failed(ex.Message);
        }
    }

    private static string? RequireType(FormValue value)
        => value is FormValue.Text { Value: var text } && text.Trim().Length == 0
            ? "Type is required."
            : null;

    private static IReadOnlyList<string> ParseTags(string value)
        => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string Text(FormField field)
        => field.GetValue() is FormValue.Text { Value: var value } ? value : "";

    private static string Text(IReadOnlyDictionary<string, FormValue> values, string id)
        => values[id] is FormValue.Text { Value: var value } ? value : "";
}
