using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Tree;

/// <summary>
/// Builds <see cref="TaskItem"/> instances with sensible defaults for
/// dependency tree tests.
/// </summary>
internal static class TaskItemBuilder
{
    public static TaskItem Create(
        string id,
        string title = "",
        string status = TaskStates.Open,
        int priority = TaskPriorities.Medium,
        string type = TaskTypes.Task)
    {
        return new TaskItem
        {
            Id = id,
            Title = title.Length == 0 ? id : title,
            Status = status,
            Priority = priority,
            Type = type,
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch
        };
    }
}
