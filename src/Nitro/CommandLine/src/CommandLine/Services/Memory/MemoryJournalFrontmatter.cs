namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// Parsed journal metadata and its markdown body.
/// </summary>
internal sealed record MemoryJournalFrontmatter(
    int Schema,
    string Id,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    string Body);
