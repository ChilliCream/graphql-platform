namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// A timestamped comment on a task.
/// </summary>
internal sealed class TaskComment
{
    /// <summary>
    /// The column list matching this type's properties, for use in SELECT
    /// statements against the comments table.
    /// </summary>
    public const string Columns =
        "id AS Id, task_id AS TaskId, author AS Author, text AS Text, created_at AS CreatedAt";

    public long Id { get; init; }
    public required string TaskId { get; init; }
    public required string Author { get; init; }
    public required string Text { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
