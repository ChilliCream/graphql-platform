using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Agents;

/// <summary>
/// Builds <see cref="TaskItem"/> instances with sensible defaults for agent
/// detail model tests.
/// </summary>
internal static class TaskItemBuilder
{
    public static TaskItem Create(
        string id,
        string status = TaskStates.Open,
        int priority = TaskPriorities.Medium,
        string type = TaskTypes.Task,
        string? assignee = null,
        DateTimeOffset? createdAt = null)
    {
        var created = createdAt ?? DateTimeOffset.UnixEpoch;

        return new TaskItem
        {
            Id = id,
            Title = id,
            Status = status,
            Priority = priority,
            Type = type,
            Assignee = assignee,
            CreatedAt = created,
            UpdatedAt = created
        };
    }
}
