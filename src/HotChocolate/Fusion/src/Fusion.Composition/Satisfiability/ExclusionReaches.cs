namespace HotChocolate.Fusion.Satisfiability;

/// <summary>
/// The reaches of a visited object when the data of some schemas is unavailable, which equals every
/// instance with the same entries in the same order.
/// </summary>
internal sealed class ExclusionReaches : IEquatable<ExclusionReaches>
{
    private readonly ExclusionReach[] _entries;
    private readonly int _hashCode;

    /// <summary>
    /// Creates the reaches with the entries of <paramref name="reaches"/>, or with none if it is
    /// <c>null</c>, followed by <paramref name="entry"/>, whose set of unavailable schemas no other entry has.
    /// </summary>
    public ExclusionReaches(ExclusionReaches? reaches, ExclusionReach entry)
    {
        var count = reaches?._entries.Length ?? 0;
        _entries = new ExclusionReach[count + 1];
        reaches?._entries.CopyTo(_entries, 0);
        _entries[count] = entry;

        var hashCode = new HashCode();

        foreach (var exclusionReach in _entries)
        {
            hashCode.Add(exclusionReach);
        }

        _hashCode = hashCode.ToHashCode();
    }

    /// <summary>
    /// Gets the entry for the schemas of <paramref name="unavailable"/>, or <c>null</c> if there is none.
    /// </summary>
    public ExclusionReach? Find(SchemaSetRef unavailable)
    {
        foreach (var entry in _entries)
        {
            if (entry.Unavailable == unavailable)
            {
                return entry;
            }
        }

        return null;
    }

    public bool Equals(ExclusionReaches? other)
        => ReferenceEquals(this, other)
            || (other is not null
                && _hashCode == other._hashCode
                && _entries.AsSpan().SequenceEqual(other._entries));

    public override bool Equals(object? obj) => obj is ExclusionReaches other && Equals(other);

    public override int GetHashCode() => _hashCode;
}
