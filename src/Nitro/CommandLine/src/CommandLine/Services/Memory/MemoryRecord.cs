namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// A curated memory's frontmatter and body, together with the absolute path
/// of the markdown file it was read from or written to.
/// </summary>
internal sealed record MemoryRecord
{
    public required string Id { get; init; }
    public required string Scope { get; init; }
    public required string Type { get; init; }
    public required IReadOnlyList<string> Tags { get; init; }
    public required string Path { get; init; }
    public required string Body { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public required string CreatedBy { get; init; }
    public string? PromotedFrom { get; init; }
}
