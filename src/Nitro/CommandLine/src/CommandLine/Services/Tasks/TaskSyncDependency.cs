namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// One outgoing dependency edge embedded in a <see cref="TaskSyncRecord"/>.
/// </summary>
internal sealed class TaskSyncDependency
{
    public required string DependsOnId { get; init; }
    public required string Type { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public string CreatedBy { get; init; } = "";
}
