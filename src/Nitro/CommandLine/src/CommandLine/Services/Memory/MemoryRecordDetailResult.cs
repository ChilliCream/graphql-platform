namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// A curated memory's full detail, as returned by the structured (JSON)
/// output of the <c>show</c> command.
/// </summary>
internal sealed record MemoryRecordDetailResult
{
    public required string Id { get; init; }
    public required string Type { get; init; }
    public required IReadOnlyList<string> Tags { get; init; }
    public required string Text { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public required string CreatedBy { get; init; }
    public string? PromotedFrom { get; init; }

    public static MemoryRecordDetailResult Create(MemoryRecord record) => new()
    {
        Id = record.Id,
        Type = record.Type,
        Tags = record.Tags,
        Text = record.Body,
        CreatedAt = record.CreatedAt,
        UpdatedAt = record.UpdatedAt,
        CreatedBy = record.CreatedBy,
        PromotedFrom = record.PromotedFrom
    };
}
