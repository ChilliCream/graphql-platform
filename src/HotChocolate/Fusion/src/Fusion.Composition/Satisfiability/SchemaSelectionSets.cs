using HotChocolate.Language;

namespace HotChocolate.Fusion.Satisfiability;

/// <summary>
/// An immutable map from schema indexes to selection sets that equals every map with the same
/// schema indexes and selection set syntax.
/// </summary>
internal sealed class SchemaSelectionSets : IEquatable<SchemaSelectionSets>
{
    private readonly int[] _schemaIndexes;
    private readonly ProvidedSelectionSetKey[] _selectionSets;
    private readonly int _hashCode;

    /// <summary>
    /// Creates a map with the given entries, which must not repeat a schema index.
    /// </summary>
    public SchemaSelectionSets(List<(int SchemaIndex, SelectionSetNode SelectionSet)> entries)
    {
        entries.Sort(static (x, y) => x.SchemaIndex.CompareTo(y.SchemaIndex));

        _schemaIndexes = new int[entries.Count];
        _selectionSets = new ProvidedSelectionSetKey[entries.Count];

        var hashCode = new HashCode();

        for (var i = 0; i < entries.Count; i++)
        {
            _schemaIndexes[i] = entries[i].SchemaIndex;
            _selectionSets[i] = new ProvidedSelectionSetKey(entries[i].SelectionSet);
            hashCode.Add(_schemaIndexes[i]);
            hashCode.Add(_selectionSets[i]);
        }

        _hashCode = hashCode.ToHashCode();
    }

    /// <summary>
    /// Gets the number of entries.
    /// </summary>
    public int Count => _schemaIndexes.Length;

    /// <summary>
    /// Gets the schema index of the entry at <paramref name="position"/>.
    /// </summary>
    public int GetSchemaIndex(int position) => _schemaIndexes[position];

    /// <summary>
    /// Gets the selection set of the entry at <paramref name="position"/>.
    /// </summary>
    public SelectionSetNode GetSelectionSet(int position) => _selectionSets[position].SelectionSet;

    /// <summary>
    /// Gets the selection set of the schema with the index <paramref name="schemaIndex"/>, or
    /// <c>null</c> if the map has none for it.
    /// </summary>
    public SelectionSetNode? Find(int schemaIndex)
    {
        var position = Array.BinarySearch(_schemaIndexes, schemaIndex);
        return position >= 0 ? _selectionSets[position].SelectionSet : null;
    }

    public bool Equals(SchemaSelectionSets? other)
        => other is not null
            && _hashCode == other._hashCode
            && _schemaIndexes.AsSpan().SequenceEqual(other._schemaIndexes)
            && _selectionSets.AsSpan().SequenceEqual(other._selectionSets);

    public override bool Equals(object? obj) => obj is SchemaSelectionSets other && Equals(other);

    public override int GetHashCode() => _hashCode;
}
