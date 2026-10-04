namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// A journal entry's core fields, as returned by the structured (JSON)
/// output of the <c>log</c> command and the bare <c>promote</c> listing.
/// </summary>
internal sealed record MemoryJournalEntryResult
{
    public required string Id { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required string CreatedBy { get; init; }

    public static MemoryJournalEntryResult Create(MemoryJournalEntry entry) => new()
    {
        Id = entry.Id,
        CreatedAt = entry.CreatedAt,
        CreatedBy = entry.CreatedBy
    };
}
