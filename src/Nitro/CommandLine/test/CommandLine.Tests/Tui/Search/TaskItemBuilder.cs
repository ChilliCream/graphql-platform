using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Search;

/// <summary>
/// Builds <see cref="TaskItem"/> instances with sensible defaults for search
/// mode tests.
/// </summary>
internal static class TaskItemBuilder
{
    public static TaskItem Create(
        string id,
        string title = "",
        string status = TaskStates.Open,
        int priority = TaskPriorities.Medium,
        string type = TaskTypes.Task,
        string? assignee = null,
        DateTimeOffset? createdAt = null,
        DateTimeOffset? updatedAt = null)
    {
        var created = createdAt ?? DateTimeOffset.UnixEpoch;

        return new TaskItem
        {
            Id = id,
            Title = title.Length == 0 ? id : title,
            Status = status,
            Priority = priority,
            Type = type,
            Assignee = assignee,
            CreatedAt = created,
            UpdatedAt = updatedAt ?? created
        };
    }
}
