namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// An immutable journal capture with its body, creation time, and author.
/// </summary>
internal sealed record MemoryJournalEntry
{
    public required string Id { get; init; }
    public required string Body { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required string CreatedBy { get; init; }
}
