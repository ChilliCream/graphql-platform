using System.Globalization;

namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// An all-primitives projection of a comment row; the intercepted read path
/// cannot convert the TEXT-stored timestamp column to DateTimeOffset.
/// </summary>
internal sealed class TaskCommentRow
{
    public long Id { get; init; }
    public required string TaskId { get; init; }
    public required string Author { get; init; }
    public required string Text { get; init; }
    public required string CreatedAt { get; init; }

    public TaskComment ToTaskComment() => new()
    {
        Id = Id,
        TaskId = TaskId,
        Author = Author,
        Text = Text,
        CreatedAt = DateTimeOffset.Parse(CreatedAt, CultureInfo.InvariantCulture)
    };
}
