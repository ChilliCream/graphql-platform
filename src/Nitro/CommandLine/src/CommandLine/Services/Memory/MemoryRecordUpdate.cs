namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// A partial update to a curated memory's fields. A <c>*Given</c> flag
/// distinguishes an omitted field from one explicitly cleared, mirroring
/// <c>TaskUpdate</c>.
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
