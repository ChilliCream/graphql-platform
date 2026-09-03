using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Tui.Board;

/// <summary>
/// Resolves the board column a task belongs to. This is the single source
/// of truth for the semantics <see cref="BoardDataLoader"/> applies to place
/// a task in the Blocked, Deferred, Ready, In Progress, or Closed column, so
/// every other surface that wants the board's status color language (the
/// graph, for one) reads the same answer instead of re-deriving it from the
/// raw <see cref="TaskItem.Status"/> field.
/// </summary>
internal static class TaskBoardStatus
{
    /// <summary>
    /// The column status for an open task that is neither blocked nor
    /// deferred. Not one of <see cref="TaskStates"/>'s well-known values,
    /// since "ready" describes a board column, not a task's own status.
    /// </summary>
    public const string Ready = "ready";

    /// <summary>
    /// Resolves the board column status for a task: <see cref="TaskStates.Closed"/>
    /// for any terminal status, <see cref="TaskStates.InProgress"/> when the
    /// task is in progress, <see cref="TaskStates.Deferred"/> when its status
    /// is deferred or its <see cref="TaskItem.DeferUntil"/> is still in the
    /// future, <see cref="TaskStates.Blocked"/> when its status is blocked or
    /// it appears in <paramref name="blocked"/>, and <see cref="Ready"/>
    /// otherwise. Deferred is checked ahead of blocked so a task waiting on
    /// both a dependency and a date lands in one column, not two.
    /// </summary>
    public static string Resolve(
        TaskItem task,
        IReadOnlyDictionary<string, IReadOnlyList<string>> blocked,
        DateTimeOffset now)
    {
        if (TaskStates.IsTerminal(task.Status))
        {
            return TaskStates.Closed;
        }

        if (task.Status == TaskStates.InProgress)
        {
            return TaskStates.InProgress;
        }

        if (task.Status == TaskStates.Deferred || (task.DeferUntil is { } deferUntil && deferUntil > now))
        {
            return TaskStates.Deferred;
        }

        if (task.Status == TaskStates.Blocked || blocked.ContainsKey(task.Id))
        {
            return TaskStates.Blocked;
        }

        return Ready;
    }
}
