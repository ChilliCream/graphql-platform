namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// Counts curated memory tags for the <c>tags</c> command.
/// </summary>
internal static class MemoryTagCounter
{
    public static IReadOnlyList<MemoryTagCount> Count(IReadOnlyList<MemoryRecord> records)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var tag in records.SelectMany(record => record.Tags))
        {
            counts[tag] = counts.GetValueOrDefault(tag) + 1;
        }

        return counts.Keys
            .OrderBy(tag => tag, StringComparer.Ordinal)
            .Select(tag => new MemoryTagCount(tag, counts[tag]))
            .ToList();
    }
}
