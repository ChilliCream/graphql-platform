using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Board;

/// <summary>
/// Builds <see cref="TaskItem"/> instances with sensible defaults for board
/// model tests.
/// </summary>
internal static class TaskItemBuilder
{
    public static TaskItem Create(
        string id,
        string status = TaskStates.Open,
        int priority = TaskPriorities.Medium,
        string type = TaskTypes.Task,
        string? assignee = null,
        DateTimeOffset? createdAt = null,
        DateTimeOffset? updatedAt = null,
        DateTimeOffset? closedAt = null,
        DateTimeOffset? deferUntil = null)
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
            UpdatedAt = updatedAt ?? created,
            ClosedAt = closedAt,
            DeferUntil = deferUntil
        };
    }
}
