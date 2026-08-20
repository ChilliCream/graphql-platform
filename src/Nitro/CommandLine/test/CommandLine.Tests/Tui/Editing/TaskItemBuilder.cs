using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Editing;

/// <summary>
/// Builds <see cref="TaskItem"/> instances with sensible defaults for task
/// lifecycle action tests.
/// </summary>
internal static class TaskItemBuilder
{
    public static TaskItem Create(
        string id,
        string title = "",
        string status = TaskStates.Open,
        int priority = TaskPriorities.Medium,
        string type = TaskTypes.Task,
        string description = "",
        string notes = "")
        => new()
        {
            Id = id,
            Title = title.Length == 0 ? id : title,
            Status = status,
            Priority = priority,
            Type = type,
            Description = description,
            Notes = notes,
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch
        };
}
