namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// A curated or journal memory record's core fields, as returned by the
/// structured (JSON) output of the memory commands.
/// </summary>
internal sealed record MemoryRecordResult
{
    public required string Id { get; init; }
    public required string Type { get; init; }
    public required IReadOnlyList<string> Tags { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public required string CreatedBy { get; init; }
    public string? PromotedFrom { get; init; }

    public static MemoryRecordResult Create(MemoryRecord record) => new()
    {
        Id = record.Id,
        Type = record.Type,
        Tags = record.Tags,
        CreatedAt = record.CreatedAt,
        UpdatedAt = record.UpdatedAt,
        CreatedBy = record.CreatedBy,
        PromotedFrom = record.PromotedFrom
    };
}
