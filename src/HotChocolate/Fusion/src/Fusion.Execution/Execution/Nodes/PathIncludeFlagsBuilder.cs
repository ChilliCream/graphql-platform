namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Builds a path include mask for operations with more than 64 include conditions.
/// Word 0 covers the condition indexes 0-63; the overflow words cover the indexes
/// 64 and above. Adding a condition returns a new builder and never mutates a shared
/// overflow array, so sibling paths forked during field collection cannot alias.
/// </summary>
internal readonly struct PathIncludeFlagsBuilder(ulong word0, ulong[]? overflow)
{
    /// <summary>
    /// Gets the mask word for the condition indexes 0-63.
    /// </summary>
    public ulong Word0 { get; } = word0;

    /// <summary>
    /// Gets the mask overflow words for the condition indexes 64 and above,
    /// or <c>null</c> if no such condition was added.
    /// </summary>
    public ulong[]? Overflow { get; } = overflow;

    /// <summary>
    /// Returns a new builder with the bit for the specified condition index set.
    /// </summary>
    public PathIncludeFlagsBuilder Add(int conditionIndex)
    {
        if (conditionIndex < 64)
        {
            return new PathIncludeFlagsBuilder(Word0 | (1ul << conditionIndex), Overflow);
        }

        var word = (conditionIndex >> 6) - 1;
        ulong[] overflow;

        if (Overflow is null || Overflow.Length <= word)
        {
            overflow = new ulong[word + 1];
            Overflow?.AsSpan().CopyTo(overflow);
        }
        else
        {
            // Copy-on-write: the current overflow array may be shared with sibling paths.
            overflow = (ulong[])Overflow.Clone();
        }

        overflow[word] |= 1ul << (conditionIndex & 63);
        return new PathIncludeFlagsBuilder(Word0, overflow);
    }
}
