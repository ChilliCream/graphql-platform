namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// The fields needed to save a new curated memory.
/// </summary>
internal sealed record MemoryRecordCreation
{
    public required string Text { get; init; }
    public required string Type { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
    public required string Actor { get; init; }
}
