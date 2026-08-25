namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// The well-known event types recorded in the task audit log.
/// </summary>
internal static class TaskEventTypes
{
    public const string Created = "created";
    public const string Updated = "updated";
    public const string StatusChanged = "status_changed";
    public const string PriorityChanged = "priority_changed";
    public const string AssigneeChanged = "assignee_changed";
    public const string Commented = "commented";
    public const string Closed = "closed";
    public const string Reopened = "reopened";
    public const string Deleted = "deleted";
    public const string DependencyAdded = "dependency_added";
    public const string DependencyRemoved = "dependency_removed";
    public const string LabelAdded = "label_added";
    public const string LabelRemoved = "label_removed";
    public const string Deferred = "deferred";
    public const string Undeferred = "undeferred";
}
