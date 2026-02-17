using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public sealed class SelectionSetIndex : ISelectionSetIndex
{
    private readonly ImmutableDictionary<SelectionSetNode, uint> _selectionSets;
    private readonly ImmutableDictionary<uint, uint> _clonedToOriginalMap;
    private readonly uint _nextId;

    internal SelectionSetIndex(
        ImmutableDictionary<SelectionSetNode, uint> selectionSets,
        ImmutableDictionary<uint, uint> clonedToOriginalMap,
        uint nextId)
    {
        _selectionSets = selectionSets;
        _clonedToOriginalMap = clonedToOriginalMap;
        _nextId = nextId;
    }

    public uint GetId(SelectionSetNode selectionSet)
        => _selectionSets[selectionSet];

    public bool TryGetOriginalIdFromCloned(uint clonedId, out uint originalId)
        => _clonedToOriginalMap.TryGetValue(clonedId, out originalId);

    public bool IsRegistered(SelectionSetNode selectionSet)
        => _selectionSets.ContainsKey(selectionSet);

    public SelectionSetIndexBuilder ToBuilder()
        => new(_selectionSets, _clonedToOriginalMap, _nextId);
}
