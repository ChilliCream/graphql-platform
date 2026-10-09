namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// A dependency edge from <see cref="TaskId"/> to <see cref="DependsOnId"/>.
/// Only one edge can exist per ordered task pair.
/// </summary>
internal sealed class TaskDependency
{
    /// <summary>
    /// The column list matching this type's properties, for use in SELECT
    /// statements against the dependencies table.
    /// </summary>
    public const string Columns =
        "task_id AS TaskId, depends_on_id AS DependsOnId, "
        + "dependency_type AS Type, created_at AS CreatedAt, created_by AS CreatedBy";

    public required string TaskId { get; init; }
    public required string DependsOnId { get; init; }
    public string Type { get; init; } = TaskDependencyTypes.Blocks;
    public required DateTimeOffset CreatedAt { get; init; }
    public string CreatedBy { get; init; } = "";
}
