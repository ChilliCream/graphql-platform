using System.Globalization;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Theming;

namespace ChilliCream.Nitro.CommandLine.Tui.Editing;

/// <summary>
/// Builds the priority quick picker for a task, and applies a picked
/// priority to the task store.
/// </summary>
internal static class PriorityPicker
{
    private static readonly (int Priority, string Label)[] WellKnownPriorities =
    [
        (TaskPriorities.Critical, "Critical"),
        (TaskPriorities.High, "High"),
        (TaskPriorities.Medium, "Medium"),
        (TaskPriorities.Low, "Low"),
        (TaskPriorities.Backlog, "Backlog")
    ];

    /// <summary>
    /// Builds the picker for <paramref name="task"/>: P0 critical through P4
    /// backlog with the priority color ramp, current value pre-selected.
    /// </summary>
    public static QuickPicker Create(TaskItem task)
        => new(
            "Priority",
            [.. WellKnownPriorities.Select(p => new QuickPickerOption(
                p.Priority.ToString(CultureInfo.InvariantCulture), RenderOption(p.Priority, p.Label)))],
            initialSelectedId: task.Priority.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Writes <paramref name="priority"/> to <paramref name="task"/>'s
    /// priority field through <see cref="ITaskStore.UpdateTaskAsync"/>.
    /// </summary>
    public static async Task<TaskEditorOutcome> ApplyAsync(
        ITaskStore store,
        TaskItem task,
        int priority,
        string actor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(task);
        ArgumentException.ThrowIfNullOrEmpty(actor);

        try
        {
            var update = new TaskUpdate
            {
                Actor = actor,
                Priority = priority,
                PriorityGiven = true
            };

            var result = await store.UpdateTaskAsync(task.Id, update, cancellationToken);

            var toastText = result.ChangedFields.Count == 0
                ? $"No changes to task '{task.Id}'."
                : $"Priority set to '{TaskPriorities.Format(priority)}' for task '{task.Id}'.";

            return new TaskEditorOutcome.Succeeded(result.ChangedFields, toastText);
        }
        catch (ExitException ex)
        {
            return new TaskEditorOutcome.Failed(ex.Message);
        }
    }

    private static string RenderOption(int priority, string label)
    {
        var style = ThemeTokens.GetStyle($"badge.priority.p{priority}").ToMarkup();
        var text = $"{TaskPriorities.Format(priority)} {label}";
        return style.Length == 0 ? Markup.Escape(text) : $"[{style}]{Markup.Escape(text)}[/]";
    }
}
