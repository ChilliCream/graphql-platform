namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// A partial curated-memory update, with flags identifying supplied text and type values.
/// An empty supplied text clears the body; null text leaves it unchanged, and an
/// empty or null supplied type is invalid.
/// </summary>
internal sealed record MemoryRecordUpdate
{
    public string? Text { get; init; }
    public bool TextGiven { get; init; }
    public string? Type { get; init; }
    public bool TypeGiven { get; init; }
    public IReadOnlyList<string> AddTags { get; init; } = [];
    public IReadOnlyList<string> RemoveTags { get; init; } = [];
}
