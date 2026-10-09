namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// The dimension <see cref="ITaskStore.CountTasksByAsync"/> groups tasks by.
/// </summary>
internal enum TaskCountDimension
{
    Status,
    Type,
    Priority,
    Assignee,
    Label
}
