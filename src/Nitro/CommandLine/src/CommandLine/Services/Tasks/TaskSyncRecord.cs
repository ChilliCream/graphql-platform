using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// One line of tasks.jsonl: a task with its labels, outgoing dependencies,
/// and comments embedded, so the line alone recreates the task. Represents
/// both live tasks and tombstones.
/// </summary>
internal sealed class TaskSyncRecord
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public string Description { get; init; } = "";
    public string Design { get; init; } = "";
    public string AcceptanceCriteria { get; init; } = "";
    public string Notes { get; init; } = "";
    public required string Status { get; init; }
    public int Priority { get; init; }
    public required string Type { get; init; }
    public string? Assignee { get; init; }
    public int? EstimatedMinutes { get; init; }
    public DateTimeOffset? DueAt { get; init; }
    public DateTimeOffset? DeferUntil { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public string CreatedBy { get; init; } = "";
    public required DateTimeOffset UpdatedAt { get; init; }
    public DateTimeOffset? ClosedAt { get; init; }
    public string CloseReason { get; init; } = "";
    public DateTimeOffset? DeletedAt { get; init; }
    public string DeleteReason { get; init; } = "";
    public IReadOnlyList<string> Labels { get; init; } = [];
    public IReadOnlyList<TaskSyncDependency> Dependencies { get; init; } = [];
    public IReadOnlyList<TaskSyncComment> Comments { get; init; } = [];
}

/// <summary>
/// AOT source-generated (de)serialization for tasks.jsonl. Kept separate
/// from <c>JsonSourceGenerationContext</c>, whose options are indented for
/// pretty-printed `--output json` results: JSONL requires exactly one
/// compact JSON object per line. Also serializes <see cref="TaskConfigEntry"/>,
/// which shares tasks.jsonl with task records as its own line per key.
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(TaskSyncRecord))]
[JsonSerializable(typeof(TaskConfigEntry))]
internal partial class TaskSyncJsonContext : JsonSerializerContext;
