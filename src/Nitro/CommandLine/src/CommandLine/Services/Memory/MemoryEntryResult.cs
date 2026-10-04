namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// A curated or journal entry summary. Journal summaries have null type, modification
/// time, and promotion source, with an empty tag list.
/// </summary>
internal sealed record MemoryEntryResult
{
    public required string Collection { get; init; }
    public required string Id { get; init; }
    public string? Type { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public required string CreatedBy { get; init; }
    public string? PromotedFrom { get; init; }

    public static MemoryEntryResult FromCurated(MemoryRecord record) => new()
    {
        Collection = MemoryCollections.Curated,
        Id = record.Id,
        Type = record.Type,
        Tags = record.Tags,
        CreatedAt = record.CreatedAt,
        UpdatedAt = record.UpdatedAt,
        CreatedBy = record.CreatedBy,
        PromotedFrom = record.PromotedFrom
    };

    public static MemoryEntryResult FromJournal(MemoryJournalEntry entry) => new()
    {
        Collection = MemoryCollections.Journal,
        Id = entry.Id,
        CreatedAt = entry.CreatedAt,
        CreatedBy = entry.CreatedBy
    };
}
