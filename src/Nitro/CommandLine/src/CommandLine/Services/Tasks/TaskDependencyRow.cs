using System.Globalization;

namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// A stored dependency edge with its timestamp represented as text.
/// </summary>
internal sealed class TaskDependencyRow
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
    public required string CreatedAt { get; init; }
    public string CreatedBy { get; init; } = "";

    public TaskDependency ToTaskDependency() => new()
    {
        TaskId = TaskId,
        DependsOnId = DependsOnId,
        Type = Type,
        CreatedAt = DateTimeOffset.Parse(CreatedAt, CultureInfo.InvariantCulture),
        CreatedBy = CreatedBy
    };
}
