namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// The promoted curated memory, as returned by the structured (JSON) output
/// of the <c>promote</c> command. <see cref="AlreadyPromoted"/> is true when
/// the journal entry had already been promoted by an earlier call: the
/// existing curated memory is returned rather than an error.
/// </summary>
internal sealed record MemoryPromotionResult
{
    public required string Id { get; init; }
    public required string Scope { get; init; }
    public required string Type { get; init; }
    public required IReadOnlyList<string> Tags { get; init; }
    public required string Path { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public required string CreatedBy { get; init; }
    public string? PromotedFrom { get; init; }
    public required bool AlreadyPromoted { get; init; }

    public static MemoryPromotionResult Create(MemoryPromotionOutcome outcome) => new()
    {
        Id = outcome.Record.Id,
        Scope = outcome.Record.Scope,
        Type = outcome.Record.Type,
        Tags = outcome.Record.Tags,
        Path = outcome.Record.Path,
        CreatedAt = outcome.Record.CreatedAt,
        UpdatedAt = outcome.Record.UpdatedAt,
        CreatedBy = outcome.Record.CreatedBy,
        PromotedFrom = outcome.Record.PromotedFrom,
        AlreadyPromoted = outcome.AlreadyPromoted
    };
}
