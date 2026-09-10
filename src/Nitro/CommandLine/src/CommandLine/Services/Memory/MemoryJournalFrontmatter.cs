namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// The parsed frontmatter and body of a journal entry markdown file. Unlike
/// <see cref="MemoryFrontmatter"/>, a journal entry has no type, tags, or
/// updated-at timestamp: it is an immutable capture, not an editable
/// curated memory.
/// </summary>
internal sealed record MemoryJournalFrontmatter(
    int Schema,
    string Id,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    string Body);
