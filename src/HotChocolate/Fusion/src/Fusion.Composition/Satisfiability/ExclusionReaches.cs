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
/// <param name="Arrival">
/// The calls of the access path that hold the object or its parent, by whether a requirement read at the
/// object without that data may take data from them.
/// </param>
internal readonly record struct ExclusionReach(
    SchemaSet Unavailable,
    SchemaSet Reach,
    SchemaSelectionSets? ReachProvides,
    bool HolderAvailable,
    SchemaSet ViewerOnly,
    ArrivalCalls Arrival);

/// <summary>
/// The calls of the access path that hold a visited object, or its parent, when the data of some schemas is
/// unavailable.
/// </summary>
/// <param name="HolderReadable">
/// <c>true</c> if a requirement check without that data may read requirement data from the holding schema and
/// the holding call is made with that data, but does not depend on the call that resolves a requiring field of
/// an unavailable schema at the object; otherwise, <c>false</c>.
/// </param>
/// <param name="HolderPending">
/// <c>true</c> if such a holding call depends on the last call of the only unavailable schema on the path, which
/// resolves its requiring field as long as that schema serves every field below it; otherwise, <c>false</c>.
/// </param>
/// <param name="ParentReadable">
/// The schema whose call holds the parent and serves the field returning the object without requirements, if a
/// requirement check without that data at the object reads requirement data from it, it is not in the reach, and
/// the call does not depend on the call that resolves the requiring field; otherwise, the empty set.
/// </param>
/// <param name="RouteReadable">
/// The schemas, other than the holding schema, that resolve the field returning the object from the parent when
/// the schema holding the parent does not serve that field without requirements, if a check without the data of a
/// single schema reads requirement data from them and they are not in the reach; otherwise, the empty set.
/// </param>
/// <param name="BelowReRootedViewer">
/// <c>true</c> if a viewer field on the path below the last node lookup is re-rooted at <c>Query.viewer</c>, in
/// which case no call is read; otherwise, <c>false</c>.
/// </param>
internal readonly record struct ArrivalCalls(
    bool HolderReadable,
    bool HolderPending,
    SchemaSet ParentReadable,
    SchemaSet RouteReadable,
    bool BelowReRootedViewer);
