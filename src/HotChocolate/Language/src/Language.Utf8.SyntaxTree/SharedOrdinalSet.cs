namespace HotChocolate.Language;

/// <summary>
/// Represents the set of document-scoped variable ordinals that a batched lookup item keeps
/// unrenamed because they are shared across items. The set is a bitmask, one bit per ordinal.
/// The default value is the empty set.
/// </summary>
internal readonly ref struct SharedOrdinalSet
{
    private readonly Span<ulong> _words;

    /// <summary>
    /// Initializes a new instance of the <see cref="SharedOrdinalSet"/> struct over
    /// <paramref name="words"/>, which is cleared so the set starts empty. The span must hold at
    /// least one bit for every ordinal that will be added or tested.
    /// </summary>
    /// <param name="words">
    /// The backing storage, one <see cref="ulong"/> per 64 ordinals.
    /// </param>
    internal SharedOrdinalSet(Span<ulong> words)
    {
        words.Clear();
        _words = words;
    }

    /// <summary>
    /// Gets the set that contains no ordinal.
    /// </summary>
    internal static SharedOrdinalSet Empty => default;

    /// <summary>
    /// Adds <paramref name="ordinal"/> to the set.
    /// </summary>
    /// <param name="ordinal">
    /// The document-scoped variable ordinal.
    /// </param>
    internal void Add(int ordinal)
        => _words[ordinal >> 6] |= 1UL << (ordinal & 63);

    /// <summary>
    /// Determines whether <paramref name="ordinal"/> is in the set.
    /// </summary>
    /// <param name="ordinal">
    /// The document-scoped variable ordinal.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the ordinal is in the set; otherwise, <see langword="false"/>.
    /// </returns>
    internal readonly bool Contains(int ordinal)
    {
        var word = ordinal >> 6;
        return word < _words.Length && (_words[word] & (1UL << (ordinal & 63))) != 0;
    }
}
