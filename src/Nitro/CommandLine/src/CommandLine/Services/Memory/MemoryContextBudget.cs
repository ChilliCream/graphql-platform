namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// The entries a <c>context</c> request admitted, and, when the very first
/// candidate alone exceeded the character budget, that candidate's id so
/// the caller can report it as omitted rather than silently returning
/// nothing.
/// </summary>
internal sealed record MemoryContextSelection(
    IReadOnlyList<MemoryRecord> Entries, string? OmittedEntryId);

/// <summary>
/// The <c>context</c> command's admission algorithm: candidates are already
/// ranked (project band first, then global; within a band, updated_at
/// descending, then id) by the caller. Whole entries are admitted in that
/// order until the entry count reaches the limit or the next entry would
/// push the canonical prompt-ready rendering, separators included, past the
/// character cap; admission then stops. An entry is never skipped,
/// partially included, or truncated to make room for a later, smaller one.
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
