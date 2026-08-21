namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// A journal entry's frontmatter and body, together with the absolute path
/// of the markdown file it was read from or written to. A journal entry
/// carries no type or tags: those are assigned only when it is promoted
/// into a curated memory.
/// </summary>
internal sealed record MemoryJournalEntry
{
    public required string Id { get; init; }
    public required string Scope { get; init; }
    public required string Path { get; init; }
    public required string Body { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required string CreatedBy { get; init; }
}
