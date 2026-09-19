using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;
using Spectre.Console.Rendering;
using Form = ChilliCream.Nitro.CommandLine.Tui.Widgets.Form.Form;

namespace ChilliCream.Nitro.CommandLine.Tui.Memory;

/// <summary>
/// Collects a required type and optional tags for promoting a journal entry.
/// Repeated promotion returns the existing curated memory.
/// </summary>
internal sealed class MemoryPromoteForm
{
    public const string TypeFieldId = "type";
    public const string TagsFieldId = "tags";

    public const string PromoteButtonId = "promote";
    public const string CancelButtonId = "cancel";

    /// <summary>
    /// The footer hints displayed while the promote form captures input.
    /// </summary>
    public static readonly IReadOnlyList<KeyHint> Hints =
    [
        new KeyHint("ctrl+s", "promote"),
        new KeyHint("esc", "cancel")
    ];

    private readonly string _journalId;
    private readonly Form _form;
    private readonly TextField _typeField;
    private readonly TextField _tagsField;

    public MemoryPromoteForm(MemoryJournalEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        _journalId = entry.Id;
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
    /// Whether either field contains text, including whitespace.
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
            var outcome = await store.PromoteAsync(_journalId, type, tags, cancellationToken)
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
