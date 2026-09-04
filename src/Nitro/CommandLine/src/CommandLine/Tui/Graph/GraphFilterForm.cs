using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;
using Spectre.Console.Rendering;
using Form = ChilliCream.Nitro.CommandLine.Tui.Widgets.Form.Form;

namespace ChilliCream.Nitro.CommandLine.Tui.Graph;

/// <summary>
/// Edits the session-only graph label and epic filters before applying them.
/// </summary>
internal sealed class GraphFilterForm
{
    public const string LabelsFieldId = "labels";
    public const string EpicIdsFieldId = "epicIds";
    public const string ApplyButtonId = "apply";
    public const string ClearButtonId = "clear";
    public const string CancelButtonId = "cancel";

    public static readonly IReadOnlyList<KeyHint> Hints =
    [
        new KeyHint("ctrl+s", "apply"),
        new KeyHint("esc", "cancel")
    ];

    private readonly Form _form;
    private readonly TextField _labels;
    private readonly TextField _epicIds;

    public GraphFilterForm(IReadOnlySet<string> labels, IReadOnlySet<string> epicIds)
    {
        _labels = new TextField(
            LabelsFieldId,
            "Labels (comma-separated, all required)",
            initialValue: string.Join(", ", labels.Order(StringComparer.Ordinal)));
        _epicIds = new TextField(
            EpicIdsFieldId,
            "Epic IDs (comma-separated, descendants included)",
            initialValue: string.Join(", ", epicIds.Order(StringComparer.Ordinal)));
        _form = new Form(
            "Graph filters",
            [_labels, _epicIds],
            new FormButtons(
            [
                new FormButtonSpec(ApplyButtonId, "Apply", ButtonKind.Primary),
                new FormButtonSpec(ClearButtonId, "Clear", ButtonKind.Secondary),
                new FormButtonSpec(CancelButtonId, "Cancel", ButtonKind.Secondary)
            ]));
    }

    public IReadOnlySet<string> Labels => Parse(_labels, normalize: true);

    public IReadOnlySet<string> EpicIds => Parse(_epicIds, normalize: false);

    public FormResult? HandleKey(ConsoleKeyInfo info) => _form.HandleKey(info);

    public IRenderable Render(int width, int height) => _form.Render(width, height);

    private static IReadOnlySet<string> Parse(TextField field, bool normalize)
    {
        var text = field.GetValue() is FormValue.Text { Value: var value } ? value : string.Empty;
        return text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => normalize ? t.Trim().ToLowerInvariant() : t.Trim())
            .Where(t => t.Length > 0)
            .ToHashSet(StringComparer.Ordinal);
    }
}
