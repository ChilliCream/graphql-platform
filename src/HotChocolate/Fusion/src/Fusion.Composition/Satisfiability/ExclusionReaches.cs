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
    public ExclusionReach? Find(SchemaSet unavailable)
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

/// <summary>
/// The reach of a visited object when the data of <paramref name="Unavailable"/> is unavailable.
/// </summary>
/// <param name="Unavailable">The schemas whose data is unavailable.</param>
/// <param name="Reach">
/// The schemas, among those a requirement check without that data may ask for, that can hold the
/// object at its position without that data.
/// </param>
/// <param name="ReachProvides">
/// The selection sets that the schemas of <paramref name="Reach"/> provide for the object, or
/// <c>null</c> if none provides one.
/// </param>
/// <param name="HolderAvailable">
/// <c>true</c> if a requirement check without that data may ask for the holding schema and that schema holds
/// the object without that data, through the call that holds it or as a schema of <paramref name="Reach"/>;
/// <c>false</c> otherwise, also when the holding call was made without that data but no such check asks for it.
/// </param>
/// <param name="ViewerOnly">
/// The schemas, among the holding schema and those of <paramref name="Reach"/>, that hold the object
/// without that data only through calls that depend on a call re-rooted at <c>Query.viewer</c>.
/// </param>
internal readonly record struct ExclusionReach(
    SchemaSet Unavailable,
    SchemaSet Reach,
    SchemaSelectionSets? ReachProvides,
    bool HolderAvailable,
    SchemaSet ViewerOnly);
