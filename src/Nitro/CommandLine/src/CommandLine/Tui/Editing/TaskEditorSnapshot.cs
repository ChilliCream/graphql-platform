using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Tui.Editing;

/// <summary>
/// The task field values a <see cref="TaskEditorForm"/> was built from, used to
/// diff the submitted values against what the store last had.
/// </summary>
internal sealed record TaskEditorSnapshot(
    string Title,
    string Status,
    int Priority,
    string Type,
    IReadOnlyList<string> Labels,
    string Description,
    string Notes)
{
    /// <summary>
    /// Captures a snapshot from a task and its labels, normalizing
    /// <paramref name="task"/>'s description and notes line endings to match
    /// what <see cref="TaskEditorForm"/>'s text areas will echo back
    /// unmodified.
    /// </summary>
    public static TaskEditorSnapshot FromTask(TaskItem task, IReadOnlyList<string> labels)
        => new(
            task.Title,
            task.Status,
            task.Priority,
            task.Type,
            labels,
            NormalizeLineEndings(task.Description),
            NormalizeLineEndings(task.Notes));

    private static string NormalizeLineEndings(string value)
        => value.Contains('\r') ? value.Replace("\r\n", "\n").Replace('\r', '\n') : value;
}
