namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// Selects whole entries in the supplied order until the count limit or rendered
/// character budget, including separators, is reached. Stops at the first entry
/// that does not fit; entries are never skipped or truncated.
/// </summary>
internal static class MemoryContextBudget
{
    public static MemoryContextSelection Select(
        IReadOnlyList<MemoryRecord> candidates, int limit, int maxChars)
    {
        var admitted = new List<MemoryRecord>();
        var renderedLength = 0;

        foreach (var candidate in candidates)
        {
            if (admitted.Count >= limit)
            {
                break;
            }

            var entryText = MemoryContextRenderer.RenderEntry(candidate);
            var addedLength = admitted.Count == 0
                ? entryText.Length
                : MemoryContextRenderer.Separator.Length + entryText.Length;

            if (renderedLength + addedLength > maxChars)
            {
                if (admitted.Count == 0)
                {
                    return new MemoryContextSelection([], candidate.Id);
                }

                break;
            }

            admitted.Add(candidate);
            renderedLength += addedLength;
        }

        return new MemoryContextSelection(admitted, null);
    }
}
