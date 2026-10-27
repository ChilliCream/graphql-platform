namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// The structured (JSON) output of the <c>context</c> command: the admitted
/// entries in rank order, or, when the first candidate alone exceeded the
/// character budget, its id as <see cref="OmittedEntryId"/> with no entries.
/// </summary>
internal sealed record MemoryContextResult
{
    public required IReadOnlyList<MemoryRecordDetailResult> Entries { get; init; }
    public string? OmittedEntryId { get; init; }

    public static MemoryContextResult Create(MemoryContextSelection selection) => new()
    {
        Entries = selection.Entries.Select(MemoryRecordDetailResult.Create).ToArray(),
        OmittedEntryId = selection.OmittedEntryId
    };
}
