using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Widgets;

namespace ChilliCream.Nitro.CommandLine.Tui.Editing;

/// <summary>
/// Builds the status quick picker for a task, and applies a picked status to
/// the task store. Picking <see cref="TaskStates.Closed"/> must not be
/// passed to <see cref="ApplyAsync"/>: the store rejects a bare status write
/// to it, and the host is expected to route that selection through the close
/// confirmation flow (<see cref="TaskLifecycleActions"/>) instead.
/// </summary>
internal static class StatusPicker
{
    private static readonly (string Id, string Label)[] WellKnownStatuses =
    [
        (TaskStates.Open, "Open"),
        (TaskStates.InProgress, "In Progress"),
        (TaskStates.Blocked, "Blocked"),
        (TaskStates.Deferred, "Deferred"),
        (TaskStates.Closed, "Closed")
    ];

    /// <summary>
    /// Builds the picker for <paramref name="task"/>: the well-known statuses
    /// with their glyphs, current value pre-selected.
    /// </summary>
    public static QuickPicker Create(TaskItem task)
        => new(
            "Status",
            [.. WellKnownStatuses.Select(status => new QuickPickerOption(status.Id, RenderOption(status.Id, status.Label)))],
            initialSelectedId: task.Status);

    /// <summary>
    /// Writes <paramref name="status"/> to <paramref name="task"/>'s status
    /// field through <see cref="ITaskStore.UpdateTaskAsync"/>.
    /// </summary>
    public static async Task<TaskEditorOutcome> ApplyAsync(
        ITaskStore store,
        TaskItem task,
        string status,
        string actor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(task);
        ArgumentException.ThrowIfNullOrEmpty(status);
        ArgumentException.ThrowIfNullOrEmpty(actor);

        try
        {
            var update = new TaskUpdate
            {
                Actor = actor,
                Status = status,
                StatusGiven = true
            };

            var result = await store.UpdateTaskAsync(task.Id, update, cancellationToken);

            var toastText = result.ChangedFields.Count == 0
                ? $"No changes to task '{task.Id}'."
                : $"Status set to '{status}' for task '{task.Id}'.";

            return new TaskEditorOutcome.Succeeded(result.ChangedFields, toastText);
        }
        catch (ExitException ex)
        {
            return new TaskEditorOutcome.Failed(ex.Message);
        }
    }

    private static string RenderOption(string status, string label)
        => $"{TaskGlyphs.StatusMarkup(status)} {Markup.Escape(label)}";
}
