namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// One comment embedded in a <see cref="TaskSyncRecord"/>.
/// </summary>
internal sealed class TaskSyncComment
{
    public long Id { get; init; }
    public required string Author { get; init; }
    public required string Text { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
