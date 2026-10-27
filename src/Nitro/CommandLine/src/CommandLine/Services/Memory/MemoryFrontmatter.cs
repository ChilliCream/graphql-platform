namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// The parsed frontmatter and body of a memory markdown file.
/// </summary>
internal sealed record MemoryFrontmatter(
    int Schema,
    string Id,
    string Type,
    IReadOnlyList<string> Tags,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string CreatedBy,
    string? PromotedFrom,
    string Body);
