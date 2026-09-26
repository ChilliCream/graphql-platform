namespace ChilliCream.Nitro.CommandLine.Tui.Memory;

/// <summary>
/// One row of the Memory tab's table: a curated memory or a journal entry tagged by
/// <see cref="Kind"/>. <see cref="Type"/> is null for a journal row, and <see cref="Time"/> is
/// the curated <c>updated_at</c> or the journal <c>created_at</c>.
/// </summary>
internal readonly record struct MemoryRow(
    MemoryCollectionFilter Kind,
    string Id,
    string? Type,
    IReadOnlyList<string> Tags,
    string Body,
    DateTimeOffset Time);
