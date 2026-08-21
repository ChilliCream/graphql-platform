namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// One curated or journal entry, as returned by the structured (JSON) list
/// output of <c>search</c> and <c>recent</c>, which can mix both
/// collections. <see cref="Type"/>, <see cref="Tags"/>, and
/// <see cref="UpdatedAt"/> are only ever populated for a curated entry: a
/// journal entry has none of them until it is promoted.
/// </summary>
internal sealed record MemoryEntryResult
{
    public required string Collection { get; init; }
    public required string Id { get; init; }
    public required string Scope { get; init; }
    public string? Type { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
    public required string Path { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public required string CreatedBy { get; init; }
    public string? PromotedFrom { get; init; }

    public static MemoryEntryResult FromCurated(MemoryRecord record) => new()
    {
        Collection = MemoryCollections.Curated,
        Id = record.Id,
        Scope = record.Scope,
        Type = record.Type,
        Tags = record.Tags,
        Path = record.Path,
        CreatedAt = record.CreatedAt,
        UpdatedAt = record.UpdatedAt,
        CreatedBy = record.CreatedBy,
        PromotedFrom = record.PromotedFrom
    };

    public static MemoryEntryResult FromJournal(MemoryJournalEntry entry) => new()
    {
        Collection = MemoryCollections.Journal,
        Id = entry.Id,
        Scope = entry.Scope,
        Path = entry.Path,
        CreatedAt = entry.CreatedAt,
        CreatedBy = entry.CreatedBy
    };
}
