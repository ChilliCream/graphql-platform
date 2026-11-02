using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Details;

/// <summary>
/// Builds <see cref="TaskItem"/> instances with sensible defaults for task
/// detail tests.
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
        string description = "",
        string design = "",
        string acceptanceCriteria = "",
        string notes = "",
        int? estimatedMinutes = null,
        DateTimeOffset? dueAt = null,
        DateTimeOffset? deferUntil = null,
        DateTimeOffset? createdAt = null,
        string createdBy = "",
        DateTimeOffset? updatedAt = null,
        DateTimeOffset? closedAt = null,
        string closeReason = "")
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
            Description = description,
            Design = design,
            AcceptanceCriteria = acceptanceCriteria,
            Notes = notes,
            EstimatedMinutes = estimatedMinutes,
            DueAt = dueAt,
            DeferUntil = deferUntil,
            CreatedAt = created,
            CreatedBy = createdBy,
            UpdatedAt = updatedAt ?? created,
            ClosedAt = closedAt,
            CloseReason = closeReason
        };
    }
}
