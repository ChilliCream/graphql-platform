using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public sealed class SelectionSetIndex : ISelectionSetIndex
{
    private readonly ImmutableDictionary<SelectionSetNode, uint> _selectionSets;
    private readonly uint _nextId;

    internal SelectionSetIndex(ImmutableDictionary<SelectionSetNode, uint> selectionSets, uint nextId)
    {
        _selectionSets = selectionSets;
        _nextId = nextId;
    }

    public uint GetId(SelectionSetNode selectionSet)
        => _selectionSets[selectionSet];

    public bool TryGetId(SelectionSetNode selectionSet, out uint id)
        => _selectionSets.TryGetValue(selectionSet, out id);

    public bool IsRegistered(SelectionSetNode selectionSet)
        => _selectionSets.ContainsKey(selectionSet);

    public SelectionSetIndexBuilder ToBuilder()
        => new(_selectionSets, _nextId);
}
