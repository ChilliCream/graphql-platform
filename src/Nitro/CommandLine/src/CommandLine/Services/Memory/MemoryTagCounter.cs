namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// Counts curated memory tags per scope for the <c>tags</c> command.
/// </summary>
internal static class MemoryTagCounter
{
    public static IReadOnlyList<MemoryTagCount> Count(IReadOnlyList<MemoryRecord> records)
    {
        var projectCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var globalCounts = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var record in records)
        {
            var counts = record.Scope == MemoryScopes.Global ? globalCounts : projectCounts;

            foreach (var tag in record.Tags)
            {
                counts[tag] = counts.GetValueOrDefault(tag) + 1;
            }
        }

        return projectCounts.Keys
            .Union(globalCounts.Keys, StringComparer.Ordinal)
            .OrderBy(tag => tag, StringComparer.Ordinal)
            .Select(tag => new MemoryTagCount(
                tag,
                projectCounts.GetValueOrDefault(tag),
                globalCounts.GetValueOrDefault(tag),
                projectCounts.GetValueOrDefault(tag) + globalCounts.GetValueOrDefault(tag)))
            .ToList();
    }
}
